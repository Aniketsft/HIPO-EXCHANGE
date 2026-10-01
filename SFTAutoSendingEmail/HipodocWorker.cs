using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AutoEmailer
{
    public class HipodocWorker : BackgroundService
    {
        private FolderMonitor _folderMonitor;
        private bool _isRunning = false;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    SystemStateManager.SetHeartbeatAndStatus(DateTime.Now.ToString("o"), _isRunning ? "RUNNING" : "STOPPED");
                    
                    var state = SystemStateManager.GetState();
                    string command = state.EngineCommand;

                    if (command == "START" && !_isRunning)
                    {
                        var profileManager = new ProfileManager();
                        var profiles = profileManager.LoadProfiles();
                        string inputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input");
                        string outputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

                        _folderMonitor = new FolderMonitor(inputFolder, outputFolder, profiles);
                        _folderMonitor.Start();
                        _isRunning = true;
                        SystemStateManager.SetHeartbeatAndStatus(DateTime.Now.ToString("o"), "RUNNING");
                    }
                    else if (command == "STOP" && _isRunning)
                    {
                        _folderMonitor?.Stop();
                        _folderMonitor = null;
                        _isRunning = false;
                        SystemStateManager.SetHeartbeatAndStatus(DateTime.Now.ToString("o"), "STOPPED");
                    }
                }
                catch (Exception ex)
                {
                    HipodocLogger.Error("Worker", $"Worker error: {ex.Message}");
                }

                await Task.Delay(5000, stoppingToken);
            }

            if (_isRunning && _folderMonitor != null)
            {
                _folderMonitor.Stop();
            }
        }
    }
}
