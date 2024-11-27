import os
import platform
import subprocess
import yaml
from pathlib import Path
import sys
import time
import argparse
import shutil
from datetime import datetime
from OnboardSoftware.radar import clearall


class ProcessManager:
    def __init__(self, log_dir="logs"):
        self.processes = []
        self.log_dir = log_dir
        os.makedirs(log_dir, exist_ok=True)
        
    def start_process(self, name, cmd, cwd=None, continuous=True):
        """
        Start a process in the background with logging
        
        Args:
            name (str): Process name
            cmd (str): Command to run
            cwd (str, optional): Working directory
            continuous (bool): Whether the process should run continuously
        """
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        log_file = os.path.join(self.log_dir, f"{name}_{timestamp}.log")
        
        try:
            with open(log_file, 'w') as f:
                process = subprocess.Popen(
                    cmd,
                    stdout=f,
                    stderr=subprocess.STDOUT,
                    cwd=cwd,
                    shell=True,
                    start_new_session=True
                )
            
            self.processes.append({
                'name': name,
                'process': process,
                'log_file': log_file,
                'cmd': cmd,
                'cwd': cwd,
                'continuous': continuous  # Track whether process should run continuously
            })
            print(f"Started {name} (PID: {process.pid})")
            print(f"Logging to: {log_file}")
            
            # If it's not a continuous process, wait for it to complete
            if not continuous:
                process.wait()
                if process.returncode != 0:
                    print(f"Error: {name} failed with return code {process.returncode}")
                    return False
                print(f"Successfully completed {name}")
            return True
            
        except Exception as e:
            print(f"Error starting {name}: {str(e)}")
            return False
            
    def stop_all(self):
        """Stop all managed processes that are meant to run continuously"""
        for p in self.processes:
            if p['continuous']:  # Only stop continuous processes
                try:
                    os.killpg(os.getpgid(p['process'].pid), signal.SIGTERM)
                    print(f"Stopped {p['name']} (PID: {p['process'].pid})")
                except Exception as e:
                    print(f"Error stopping {p['name']}: {str(e)}")


def find_conda():
    """Find the conda executable path"""
    # conda_path = shutil.which('conda')
    # if conda_path:
        # return conda_path
        
    possible_paths = [
        os.path.expanduser('~/anaconda3/bin/conda'),
        os.path.expanduser('~/miniconda3/bin/conda'),
        os.path.expanduser('~/miniforge3/bin/conda'),
        "/opt/miniconda3/bin/conda",
        "/bin/conda",
        "/usr/bin/conda",
        '/opt/conda/bin/conda',
        'C:\\ProgramData\\Anaconda3\\Scripts\\conda.exe',
        'C:\\ProgramData\\Miniconda3\\Scripts\\conda.exe',
    ]
    
    for path in possible_paths:
        if os.path.exists(path):
            return path
            
    return None

def get_python_cmd(conda_env):
    """Get the correct Python command for the conda environment"""
    conda_path = find_conda()
    if not conda_path:
        print("Error: Could not find conda installation. Please ensure conda is installed.")
        sys.exit(1)
        
    try:
        result = subprocess.run([conda_path, 'env', 'list'], 
                              capture_output=True, 
                              text=True)
        if conda_env not in result.stdout:
            print(f"Error: Conda environment '{conda_env}' not found.")
            print("Available environments:")
            print(result.stdout)
            sys.exit(1)
            
        env_path = None
        for line in result.stdout.splitlines():
            if conda_env in line:
                env_path = line.split()[-1]
                break
                
        if not env_path:
            print(f"Error: Could not determine path for environment '{conda_env}'")
            sys.exit(1)
            
        is_windows = platform.system().lower() == 'windows'
        python_path = os.path.join(env_path, 'python.exe' if is_windows else 'bin/python')
        
        if not os.path.exists(python_path):
            print(f"Error: Python executable not found at {python_path}")
            sys.exit(1)
            
        return python_path
        
    except subprocess.CalledProcessError as e:
        print(f"Error running conda command: {str(e)}")
        sys.exit(1)

def load_config(config_path):
    """Load configuration from YAML file"""
    try:
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
            
        required_keys = ['onboard_path', 'viz_path', 'api_path', 'db_path', 'conda_env']
        for key in required_keys:
            if key not in config:
                raise KeyError(f"Missing required configuration key: {key}")
                
        # Convert paths to absolute paths
        for key in ['onboard_path', 'viz_path', 'api_path', 'db_path']:
            config[key] = os.path.abspath(os.path.expanduser(config[key]))
            
        return config
    except Exception as e:
        print(f"Error loading configuration file: {str(e)}")
        sys.exit(1)

def main():
    parser = argparse.ArgumentParser(description='Start services with multiple onboard instances')
    parser.add_argument('config', help='Path to configuration YAML file')
    parser.add_argument('-n', '--num-instances', type=int, default=1,
                       help='Number of onboard software instances to start')
    parser.add_argument('--log-dir', default='logs',
                       help='Directory for log files')
    args = parser.parse_args()
    
    config = load_config(args.config)
    python_path = get_python_cmd(config['conda_env'])
    print(f"Using Python from: {python_path}")
    
    # Set PYTHONPATH
    project_root = os.path.dirname(os.path.abspath(__file__))
    os.environ['PYTHONPATH'] = f"{project_root}:{os.environ.get('PYTHONPATH', '')}"
    
    # Initialize process manager
    pm = ProcessManager(log_dir=args.log_dir)    
    # Start database
    if not pm.start_process(
        'database',
        'docker compose up -d',
        cwd=config['db_path'],
        continuous=False
    ):
        sys.exit(1)
    
    print("Waiting for database to initialize...")
    time.sleep(config['db_startup_delay'])
    
    # Start API
    if not pm.start_process(
        'api',
        f"{python_path} {config['api_path']}",
    ):
        sys.exit(1)

    time.sleep(2)
    clearall()
    

    # Start onboard instances
    for i in range(args.num_instances):
        if not pm.start_process(
            f'onboard_{i}',
            f"{python_path} {config['onboard_name']} -r {i} {'-v' if config['plot_ppi'] else ' '} {'--model ' + config['model_path'] if config['model_path'] else ''}",
            f"{config['onboard_path']}"
        ):
            pm.stop_all()
            sys.exit(1)
        time.sleep(2)
    
    # Start visualization platform
    if not pm.start_process(
        'visualization',
        'npm run dev',
        cwd=config['viz_path']
    ):
        pm.stop_all()
        sys.exit(1)
    
    print(f"\nAll services started successfully with {args.num_instances} onboard instance(s)!")
    print("Process status:")
    for p in pm.processes:
        print(f"- {p['name']}: PID {p['process'].pid}, Log: {p['log_file']}")
    
    print("\nPress Ctrl+C to stop all services...")
    
    try:
        while True:
            # Check if any process has terminated
            for p in pm.processes:
                if p['process'].poll() is not None and p['continuous']:
                    print(f"\nWarning: {p['name']} (PID: {p['process'].pid}) has terminated!")
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nShutting down services...")
        pm.stop_all()
        print("Services shutdown complete")

if __name__ == "__main__":
    import signal
    main()
