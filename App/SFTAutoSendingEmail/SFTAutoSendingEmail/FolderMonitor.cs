using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;

namespace AutoEmailer
{
    public class FolderMonitor
    {
        private readonly PdfProcessor _pdfProcessor;
        private readonly EmailSender _emailSender;
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly QueueEngine _queueEngine;
        private readonly List<RptProfile> _profiles;
        private System.Timers.Timer _masterTimer;
        private DateTime _lastLicenseCheck = DateTime.UtcNow;

        public event Action<string> OnLog;

        public FolderMonitor(string inputFolder, string outputFolder, List<RptProfile> profiles)
        {
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _profiles = profiles;
            _pdfProcessor = new PdfProcessor();
            _emailSender = new EmailSender(profiles);
            _queueEngine = new QueueEngine();

            _masterTimer = new System.Timers.Timer(10000); // 10 seconds
            _masterTimer.Elapsed += OnMasterTimerElapsed;
        }

        public void Start()
        {
            if (!Directory.Exists(_inputFolder)) Directory.CreateDirectory(_inputFolder);
            if (!Directory.Exists(_outputFolder)) Directory.CreateDirectory(_outputFolder);
            string stagingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Staging");
            if (!Directory.Exists(stagingDir)) Directory.CreateDirectory(stagingDir);

            Log($"Started monitoring folder: {_inputFolder}");
            _masterTimer.Start();
        }

        public void Stop()
        {
            _masterTimer.Stop();
            Log("Stopped monitoring.");
        }

        private async void OnMasterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _masterTimer.Stop();
            try
            {
                if ((DateTime.UtcNow - _lastLicenseCheck).TotalHours >= 24)
                {
                    try
                    {
                        LicenseValidator.Validate();
                        _lastLicenseCheck = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        Log($"CRITICAL LICENSE ERROR: {ex.Message}");
                        Environment.Exit(-1);
                    }
                }

                // Phase 1: Ingestion
                if (Directory.Exists(_inputFolder))
                {
                    var pdfFiles = Directory.GetFiles(_inputFolder, "*.pdf");
                    foreach (var file in pdfFiles)
                    {
                        try
                        {
                            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                            }
                        }
                        catch (IOException)
                        {
                            Log($"File is locked by X3, skipping...");
                            continue;
                        }

                        try
                        {
                            Log($"Starting PDF split and metadata extraction for: {file}");
                            _pdfProcessor.ProcessBatchPdf(file, _profiles, _queueEngine);
                            Log($"Successfully queued batch PDF for sending.");
                        }
                        catch (Exception ex)
                        {
                            Log($"ERROR processing file {Path.GetFileName(file)}: {ex.Message}");
                            HipodocLogger.Error("FolderMonitor", $"ERROR processing file {Path.GetFileName(file)}: {ex.Message}");
                            try
                            {
                                string failedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Failed");
                                if (!Directory.Exists(failedPath)) Directory.CreateDirectory(failedPath);
                                string destFile = Path.Combine(failedPath, Path.GetFileName(file));
                                if (File.Exists(destFile)) File.Delete(destFile);
                                File.Move(file, destFile);
                                Log($"Moved failed batch to: {destFile}");

                                RetryEngine.AddFailed(destFile, "BATCH");
                            }
                            catch (Exception moveEx)
                            {
                                Log($"ERROR moving file to Failed folder: {moveEx.Message}");
                            }
                        }
                    }
                }

                // Phase 2: Dispatch
                var pending = _queueEngine.GetPendingEmails();
                if (pending == null || pending.Count == 0) return;

                var grouped = pending
                    .Where(p => !string.IsNullOrEmpty(p.EmailAddress))
                    .GroupBy(p => new { p.EmailAddress, p.ProfileId })
                    .ToList();

                foreach (var group in grouped)
                {
                    bool success = false;
                    try
                    {
                        var groupList = group.ToList();
                        await _emailSender.SendQueueEmailsAsync(groupList);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to send emails for {group.Key.EmailAddress}: {ex.Message}");
                    }

                    string destDir = success
                        ? Path.Combine(_outputFolder, "Archive", DateTime.Now.ToString("yyyy-MM-dd"))
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Failed");

                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    foreach (var record in group)
                    {
                        try
                        {
                            if (File.Exists(record.FilePath))
                            {
                                string destFile = Path.Combine(destDir, Path.GetFileName(record.FilePath));
                                if (File.Exists(destFile)) File.Delete(destFile);
                                File.Move(record.FilePath, destFile);

                                if (!success)
                                {
                                    RetryEngine.AddFailed(destFile, record.ProfileId);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error moving file {record.FilePath}: {ex.Message}");
                        }
                        finally
                        {
                            _queueEngine.RemoveRecord(record.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error in master timer: {ex.Message}");
            }
            finally
            {
                _masterTimer.Start();
            }
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            HipodocLogger.Info("FolderMonitor", message);
        }
    }
}
