
from random import randint
from augmentations import AugmentedRadarDataset
import os
import torch
import torch.nn as nn
from torch.utils.data import DataLoader, random_split
import matplotlib.pyplot as plt
from dataset import PPIDataset
from centernetResnet import CenterNetBackbone, FocalLoss, detect_points
import logging
import datetime
import numpy as np


def setup_logging():
    # Create logs directory if it doesn't exist
    if not os.path.exists('logs'):
        os.makedirs('logs')

    # Set up logging
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    logging.basicConfig(
        filename=f'logs/training_{timestamp}.log',
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s'
    )


def visualize_predictions(model, dataset, device, num_samples=5, threshold=0.3):
    """
    Visualize model predictions with detected points
    """
    model.eval()

    # Ensure we don't try to visualize more samples than we have
    num_samples = min(num_samples, len(dataset))
    if num_samples == 0:
        logging.warning("No samples available for visualization")
        return

    fig, axes = plt.subplots(num_samples, 3, figsize=(15, 5*num_samples))

    # Handle case where we only have one sample
    if num_samples == 1:
        axes = axes.reshape(1, -1)

    for i in range(num_samples):

        image, target_heatmap = dataset[randint(0, len(dataset)-1)]

        # Add batch dimension if necessary
        if image.dim() == 3:
            image = image.unsqueeze(0)

        # Get predictions
        with torch.no_grad():
            pred_heatmap = model(image.to(device))
            pred_heatmap = pred_heatmap[0, 0].cpu()

        # Detect points from both target and predicted heatmaps
        target_points = detect_points(target_heatmap[0], threshold=threshold)
        pred_points = detect_points(pred_heatmap, threshold=threshold)
        # Absurdly high number of predicted points, skip
        if len(pred_points) > len(target_points)*5:
            continue

        # Plot original image with target points
        axes[i, 0].imshow(image[0, 0].cpu(), cmap='gray')
        axes[i, 0].set_title('Original Image with Ground Truth')
        for x, y in target_points:
            axes[i, 0].plot(x, y, 'r+', markersize=10, markeredgewidth=2)
        axes[i, 0].axis('on')

        # Plot target heatmap with points
        axes[i, 1].imshow(target_heatmap[0], cmap='hot', alpha=0.7)
        for x, y in target_points:
            axes[i, 1].plot(x, y, 'r+', markersize=10, markeredgewidth=2)
        axes[i, 1].set_title('Target Heatmap')
        axes[i, 1].axis('on')

        # Plot predicted heatmap with points
        axes[i, 2].imshow(pred_heatmap, cmap='hot', alpha=0.7)
        for x, y in pred_points:
            axes[i, 2].plot(x, y, 'g+', markersize=10, markeredgewidth=2)
        axes[i, 2].set_title(f'Predicted Heatmap ({len(pred_points)} ships)')
        axes[i, 2].axis('on')

        # Add text with number of ships detected
        plt.figtext(0.02, 1.0 - (i/num_samples),
                    f'Sample'
                    f'{i+1}:' f'{len(target_points)}'
                    f'ships (GT), {len(pred_points)} ships (Pred)',
                    fontsize=10)

    plt.tight_layout()

    # Create plots directory if it doesn't exist
    if not os.path.exists('plots'):
        os.makedirs('plots')

    # Save with timestamp
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    save_path = f'plots/predictions_{timestamp}.png'
    plt.savefig(save_path, bbox_inches='tight', dpi=150)
    plt.close()

    logging.info(f'Saved visualization to {save_path}')


def evaluate_model(model, dataset, device, threshold=0.3):
    """
    Evaluate model performance on the dataset
    """
    model.eval()
    total_correct = 0
    total_pred = 0
    total_true = 0

    with torch.no_grad():
        for i in range(len(dataset)):
            image, target_heatmap = dataset[i]

            # Add batch dimension if necessary
            if image.dim() == 3:
                image = image.unsqueeze(0)

            # Get predictions
            pred_heatmap = model(image.to(device))
            pred_heatmap = pred_heatmap[0, 0].cpu()

            # Detect points
            target_points = detect_points(
                target_heatmap[0], threshold=threshold)
            pred_points = detect_points(
                pred_heatmap, threshold=threshold)

            # Count matches (using a simple distance threshold)
            matched_points = set()
            for pred_x, pred_y in pred_points:
                for idx, (true_x, true_y) in enumerate(target_points):
                    if idx not in matched_points:
                        dist = np.sqrt((pred_x - true_x)**2 +
                                       (pred_y - true_y)**2)
                        if dist < 10:  # Distance threshold in pixels
                            total_correct += 1
                            matched_points.add(idx)
                            break

            total_pred += len(pred_points)
            total_true += len(target_points)

    precision = total_correct / total_pred if total_pred > 0 else 0
    recall = total_correct / total_true if total_true > 0 else 0
    f1 = 2 * (precision * recall) / (precision +
                                     recall) if (precision + recall) > 0 else 0

    return {
        'precision': precision,
        'recall': recall,
        'f1': f1,
        'true_positives': total_correct,
        'false_positives': total_pred - total_correct,
        'false_negatives': total_true - total_correct
    }


def train(model, train_loader, val_loader, criterion, optimizer, scheduler, num_epochs, device, patience):
    best_val_loss = float('inf')
    early_stop_grace = 0
    prev_lr = optimizer.param_groups[0]['lr']
    best_model_state = None

    for epoch in range(num_epochs):
        # Training phase
        model.train()
        epoch_loss = 0
        batch_count = 0

        for batch_idx, (images, targets) in enumerate(train_loader):
            images = images.to(device)
            targets = targets.to(device)

            optimizer.zero_grad()
            outputs = model(images)
            loss = criterion(outputs, targets)

            loss.backward()
            optimizer.step()

            epoch_loss += loss.item()
            batch_count += 1

            if batch_idx % 100 == 0:
                logging.info(f"Epoch {epoch+1}/{num_epochs}, Batch {batch_idx}/{len(train_loader)}, "
                             f"Loss: {loss.item():.4f}")

            del images, targets, outputs, loss
            if device == 'cuda':
                torch.cuda.empty_cache()

        avg_train_loss = epoch_loss / batch_count

        # Validation phase
        model.eval()
        val_loss = 0
        val_batch_count = 0

        with torch.no_grad():
            for images, targets in val_loader:
                images = images.to(device)
                targets = targets.to(device)

                outputs = model(images)
                batch_loss = criterion(outputs, targets)

                val_loss += batch_loss.item()
                val_batch_count += 1

                del images, targets, outputs, batch_loss
                if device == 'cuda':
                    torch.cuda.empty_cache()

        avg_val_loss = val_loss / val_batch_count

        # Step the scheduler
        current_lr = optimizer.param_groups[0]['lr']
        scheduler.step(avg_val_loss)
        new_lr = optimizer.param_groups[0]['lr']

        # Check if learning rate decreased
        if new_lr < current_lr:
            logging.info(f'Learning rate decreased from {current_lr:.6f} to {new_lr:.6f}')
            
            # Load the best model state if we have one
            if best_model_state is not None:
                logging.info('Loading best model state after learning rate reduction')
                model.load_state_dict(best_model_state['model_state_dict'])
                optimizer.load_state_dict(best_model_state['optimizer_state_dict'])
                # Update the optimizer's learning rate to the new reduced value
                for param_group in optimizer.param_groups:
                    param_group['lr'] = new_lr
                
                # Reset early stopping counter since we're starting from best point
                early_stop_grace = 0

        # Evaluate detection performance
        if (epoch + 1) % 5 == 0:
            metrics = evaluate_model(model, val_loader.dataset, device)
            logging.info(f'Evaluation metrics - '
                         f'Precision: {metrics["precision"]:.3f}, '
                         f'Recall: {metrics["recall"]:.3f}, '
                         f'F1: {metrics["f1"]:.3f}')

            visualize_predictions(model, val_loader.dataset, device)

        logging.info(f'Epoch {epoch+1}/{num_epochs}, '
                     f'Training Loss: {avg_train_loss:.4f}, '
                     f'Validation Loss: {avg_val_loss:.4f}, '
                     f'LR: {new_lr:.6f}')

        # Save best model
        if avg_val_loss < best_val_loss:
            early_stop_grace = 0
            best_val_loss = avg_val_loss
            
            # Save both to disk and keep in memory
            best_model_state = {
                'epoch': epoch,
                'model_state_dict': model.state_dict(),
                'optimizer_state_dict': optimizer.state_dict(),
                'scheduler_state_dict': scheduler.state_dict(),
                'loss': best_val_loss,
                'metrics': metrics if (epoch + 1) % 5 == 0 else None,
            }
            
            torch.save(best_model_state, 'best_model.pth')
            logging.info(f'Saved new best model with validation loss: {best_val_loss:.4f}')
        else:
            early_stop_grace += 1

        if early_stop_grace > patience:
            logging.info("Reached early stopping criteria")
            return

def main():
    # Setup
    setup_logging()
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    logging.info(f'Using device: {device}')

    # Hyperparameters
    BATCH_SIZE = 4
    NUM_EPOCHS = 500
    INITIAL_LR = 1e-2
    SIGMA = 2
    PATIENCE = 10

    # Learning rate scheduler parameters
    LR_FACTOR = 0.5        # Factor to multiply learning rate by when decreasing
    LR_PATIENCE = 5        # Number of epochs with no improvement after which LR will decrease
    LR_MIN = 1e-7         # Minimum learning rate
    LR_THRESHOLD = 1e-4   # Minimum change in validation loss to qualify as an improvement

    # Dataset setup
    json_directory = os.path.expanduser(r'D:\Datasets\MYA')
    base_dataset = PPIDataset(json_directory, sigma=SIGMA)

    # Split dataset before augmentation
    train_size = int(0.8 * len(base_dataset))
    val_size = len(base_dataset) - train_size

    train_dataset_base, val_dataset = random_split(
        base_dataset, [train_size, val_size])

    # Create augmented training dataset
    train_dataset = AugmentedRadarDataset(
        train_dataset_base,
        flip_prob=0.5,
        num_shifts=3,
        shift_fraction=0.15
    )

    logging.info(
        f'Original dataset size - Train: {len(train_dataset_base)}, Validation: {len(val_dataset)}')
    logging.info(f'Augmented training dataset size: {len(train_dataset)}')

    # Create data loaders
    train_loader = DataLoader(
        train_dataset_base,
        batch_size=BATCH_SIZE,
        shuffle=True,
        num_workers=2,
        pin_memory=True if device == 'cuda' else False
    )

    val_loader = DataLoader(
        val_dataset,
        batch_size=BATCH_SIZE,
        num_workers=2,
        pin_memory=True if device == 'cuda' else False
    )

    # Model setup
    model = CenterNetBackbone(in_channels=1).to(device)
    criterion = FocalLoss()
    optimizer = torch.optim.Adam(model.parameters(), lr=INITIAL_LR)

    # Learning rate scheduler
    scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(
        optimizer,
        mode='min',              # Monitor minimization of the validation loss
        factor=LR_FACTOR,        # Multiply LR by this factor when reducing
        patience=LR_PATIENCE,    # Number of epochs to wait before reducing LR
        verbose=True,            # Print message when LR is reduced
        min_lr=LR_MIN,          # Don't reduce LR below this value
        threshold=LR_THRESHOLD,  # Minimum change in loss to qualify as an improvement
    )

    logging.info(f'Initial learning rate: {INITIAL_LR}')
    logging.info(f'Learning rate schedule - Factor: {LR_FACTOR}, Patience: {LR_PATIENCE}, '
                 f'Min LR: {LR_MIN}, Threshold: {LR_THRESHOLD}')

    # Train model
    try:
        train(model, train_loader, val_loader, criterion,
              optimizer, scheduler, NUM_EPOCHS, device, PATIENCE)
        logging.info('Training completed successfully!')

        # Final evaluation
        metrics = evaluate_model(model, val_dataset, device)
        logging.info(f'Final evaluation metrics:')
        logging.info(f'Precision: {metrics["precision"]:.3f}')
        logging.info(f'Recall: {metrics["recall"]:.3f}')
        logging.info(f'F1: {metrics["f1"]:.3f}')

    except Exception as e:
        logging.error(f'Error during training: {str(e)}')
        raise


if __name__ == '__main__':
    main()
