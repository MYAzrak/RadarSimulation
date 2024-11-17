import torch
import torch.nn as nn
from torch.utils.data import DataLoader
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np
import os
import logging
import datetime
from dataset import PPIDataset
from centernetresnet import CenterNetBackbone, detect_points
import json

def setup_logging():
    if not os.path.exists('test_logs'):
        os.makedirs('test_logs')
    
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    logging.basicConfig(
        filename=f'test_logs/test_results_{timestamp}.log',
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s'
    )

def load_model(model_path, device):
    """Load the trained model"""
    model = CenterNetBackbone(in_channels=1).to(device)
    checkpoint = torch.load(model_path, map_location=device)
    model.load_state_dict(checkpoint['model_state_dict'])
    model.eval()
    return model

def calculate_metrics(predictions, ground_truth, distance_threshold=10):
    """
    Calculate TP, FP, FN for ship detection based on point distances
    """
    true_positives = 0
    matched_gt_points = set()
    
    # For each predicted point, find the closest ground truth point
    for pred_point in predictions:
        min_dist = float('inf')
        closest_gt_idx = None
        
        for idx, gt_point in enumerate(ground_truth):
            if idx not in matched_gt_points:
                dist = np.sqrt((pred_point[0] - gt_point[0])**2 + 
                             (pred_point[1] - gt_point[1])**2)
                if dist < min_dist:
                    min_dist = dist
                    closest_gt_idx = idx
        
        if closest_gt_idx is not None and min_dist <= distance_threshold:
            true_positives += 1
            matched_gt_points.add(closest_gt_idx)
    
    false_positives = len(predictions) - true_positives
    false_negatives = len(ground_truth) - true_positives
    
    return true_positives, false_positives, false_negatives

def save_predictions(model, dataset, device, save_dir='test_predictions', num_samples=10):
    """Save visualization of model predictions"""
    if not os.path.exists(save_dir):
        os.makedirs(save_dir)
    
    indices = np.random.choice(len(dataset), min(num_samples, len(dataset)), replace=False)
    
    for idx in indices:
        image, target_heatmap = dataset[idx]
        
        # Add batch dimension
        image = image.unsqueeze(0)
        
        # Get predictions
        with torch.no_grad():
            pred_heatmap = model(image.to(device))
            pred_heatmap = pred_heatmap[0, 0].cpu()
        
        # Detect points
        target_points = detect_points(target_heatmap[0], threshold=0.3)
        pred_points = detect_points(pred_heatmap, threshold=0.3)
        
        # Calculate metrics for this image
        tp, fp, fn = calculate_metrics(pred_points, target_points)
        
        # Create visualization
        fig, axes = plt.subplots(1, 3, figsize=(15, 5))
        
        # Original image with ground truth
        axes[0].imshow(image[0, 0].cpu(), cmap='gray')
        axes[0].set_title('Original with Ground Truth')
        for x, y in target_points:
            axes[0].plot(x, y, 'r+', markersize=10)
        
        # Target heatmap
        axes[1].imshow(target_heatmap[0], cmap='hot')
        axes[1].set_title('Target Heatmap')
        # for x, y in target_points:
        #     axes[1].plot(x, y, 'r+', markersize=10)
        
        # Predicted heatmap
        axes[2].imshow(pred_heatmap.cpu(), cmap='hot')
        axes[2].set_title('Predicted Heatmap')
        for x, y in pred_points:
            axes[2].plot(x, y, 'g+', markersize=10)
        
        plt.suptitle(f'Sample {idx}: TP={tp}, FP={fp}, FN={fn}')
        plt.tight_layout()
        
        # Save figure
        plt.savefig(f'{save_dir}/sample_{idx}.png')
        plt.close()

def plot_detection_metrics(tp, fp, fn, save_dir='test_results'):
    """Plot detection metrics using ConfusionMatrixDisplay"""
    from sklearn.metrics import ConfusionMatrixDisplay
    
    # Create confusion matrix
    cm = np.array([
        [tp, fp],
        [fn, 0]
    ])
    
    # Create and customize the display
    disp = ConfusionMatrixDisplay(
        confusion_matrix=cm,
        display_labels=['Ship', 'Background']
    )
    
    fig, ax = plt.subplots(figsize=(8, 6))
    disp.plot(ax=ax, cmap='Blues', values_format='d')
    plt.title('Detection Results')
    plt.tight_layout()
    plt.savefig(f'{save_dir}/detection_metrics.png')
    plt.close()
def evaluate_model(model, test_loader, device, save_dir='test_results'):
    """Comprehensive model evaluation"""
    if not os.path.exists(save_dir):
        os.makedirs(save_dir)
    
    all_tp = 0
    all_fp = 0
    all_fn = 0
    
    for i, (image, target_heatmap) in enumerate(test_loader.dataset):
        # Add batch dimension
        image = image.unsqueeze(0)
        
        # Get predictions
        with torch.no_grad():
            pred_heatmap = model(image.to(device))
            pred_heatmap = pred_heatmap[0, 0].cpu()
        
        # Detect points
        target_points = detect_points(target_heatmap[0], threshold=0.3)
        pred_points = detect_points(pred_heatmap, threshold=0.3)
        
        # Calculate metrics
        tp, fp, fn = calculate_metrics(pred_points, target_points)
        all_tp += tp
        all_fp += fp
        all_fn += fn
    
    # Calculate final metrics
    precision = all_tp / (all_tp + all_fp) if (all_tp + all_fp) > 0 else 0
    recall = all_tp / (all_tp + all_fn) if (all_tp + all_fn) > 0 else 0
    f1 = 2 * (precision * recall) / (precision + recall) if (precision + recall) > 0 else 0
    
    # Plot detection metrics
    plot_detection_metrics(int(all_tp), int(all_fp), int(all_fn), save_dir)
    
    # Save metrics
    metrics = {
        'precision': float(precision),
        'recall': float(recall),
        'f1_score': float(f1),
        'true_positives': int(all_tp),
        'false_positives': int(all_fp),
        'false_negatives': int(all_fn)
    }
    
    with open(f'{save_dir}/metrics.json', 'w') as f:
        json.dump(metrics, f, indent=4)
    
    return metrics

def main():
    # Setup
    setup_logging()
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    logging.info(f'Using device: {device}')
    
    # Load dataset
    json_directory = os.path.expanduser('~/RadarDataSubset/')
    test_dataset = PPIDataset(json_directory, sigma=2)
    test_loader = DataLoader(test_dataset, batch_size=1, shuffle=False)
    
    # Load model
    model = load_model('best_model.pth', device)
    
    try:
        # Evaluate model
        logging.info('Starting model evaluation...')
        metrics = evaluate_model(model, test_loader, device)
        
        # Log results
        logging.info('Evaluation Results:')
        logging.info(f'Precision: {metrics["precision"]:.3f}')
        logging.info(f'Recall: {metrics["recall"]:.3f}')
        logging.info(f'F1 Score: {metrics["f1_score"]:.3f}')
        logging.info(f'True Positives: {metrics["true_positives"]}')
        logging.info(f'False Positives: {metrics["false_positives"]}')
        logging.info(f'False Negatives: {metrics["false_negatives"]}')
        
        # Save prediction examples
        logging.info('Saving prediction examples...')
        save_predictions(model, test_dataset, device)
        
        logging.info('Testing completed successfully!')
        
    except Exception as e:
        logging.error(f'Error during testing: {str(e)}')
        raise

if __name__ == '__main__':
    main()
