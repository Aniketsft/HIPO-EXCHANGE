using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AutoEmailer
{
    public class ExtractedDocument
    {
        public string FilePath { get; set; }
        public string EmailAddress { get; set; }
        public string DocumentName { get; set; }
        public string ProfileType { get; set; }
        public Dictionary<string, string> DynamicVariables { get; set; } = new Dictionary<string, string>();
    }

    public class PdfProcessor
    {
        // Example tag: @@HIPO@@|EMAIL:abc@test.com|FILENAME:INV-1002|@@ENDHIPO@@
        private static readonly Regex MetaRegex = new Regex(@"@@HIPO@@(.*?)@@ENDHIPO@@", RegexOptions.Compiled);

        public void ProcessBatchPdf(string batchPdfPath, List<RptProfile> profiles, QueueEngine queueEngine)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string stagingDir = Path.Combine(baseDir, "Staging");
            string processedDir = Path.Combine(baseDir, "Processed_Batches");

            if (!Directory.Exists(stagingDir)) Directory.CreateDirectory(stagingDir);
            if (!Directory.Exists(processedDir)) Directory.CreateDirectory(processedDir);

            using (var pdfDocument = PdfSharp.Pdf.IO.PdfReader.Open(batchPdfPath, PdfDocumentOpenMode.Import))
            {
                using (var pdfPigDocument = UglyToad.PdfPig.PdfDocument.Open(batchPdfPath))
                {
                    PdfSharp.Pdf.PdfDocument currentSplitDoc = null;
                    string currentEmail = null;
                    string currentFileName = null;
                    string currentGroupKey = null;
                    string currentType = null;
                    string currentPassword = null;
                    Dictionary<string, string> currentVariables = new Dictionary<string, string>();

                    for (int i = 0; i < pdfDocument.PageCount; i++)
                    {
                        var pigPage = pdfPigDocument.GetPage(i + 1);
                        string pageText = pigPage.Text;

                        var match = MetaRegex.Match(pageText);
                        
                        string pageEmail = null;
                        string pageFileName = null;
                        string pageGroupKey = null;
                        string pageType = null;
                        string pagePassword = null;
                        var pageVariables = new Dictionary<string, string>();

                        if (match.Success)
                        {
                            string metaContent = match.Groups[1].Value;
                            var parts = metaContent.Split('|', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var part in parts)
                            {
                                int colonIdx = part.IndexOf(':');
                                if (colonIdx < 0) continue;
                                
                                string key = part.Substring(0, colonIdx);
                                string val = part.Substring(colonIdx + 1);

                                if (key == "EMAIL") pageEmail = val;
                                else if (key == "FILENAME") pageFileName = val;
                                else if (key == "GROUP") pageGroupKey = val;
                                else if (key == "TYPE") pageType = val;
                                else if (key == "PASSWORD") pagePassword = val;
                                else pageVariables[key] = val; // Store as dynamic variable
                            }
                        }

                        if (string.IsNullOrEmpty(pageGroupKey))
                        {
                            pageGroupKey = pageFileName;
                        }

                        bool isNewGroup = !string.IsNullOrEmpty(pageGroupKey) && pageGroupKey != currentGroupKey;

                        if (isNewGroup || (i == 0 && currentSplitDoc == null))
                        {
                            if (currentSplitDoc != null && !string.IsNullOrEmpty(currentFileName))
                            {
                                SaveAndQueueDocument(currentSplitDoc, stagingDir, currentFileName, currentEmail, currentType, currentPassword, profiles, queueEngine);
                            }

                            currentSplitDoc = new PdfSharp.Pdf.PdfDocument();
                            currentEmail = pageEmail;
                            currentFileName = pageFileName;
                            currentGroupKey = pageGroupKey;
                            currentType = pageType;
                            currentPassword = pagePassword;
                            currentVariables = pageVariables;
                        }

                        if (currentSplitDoc != null)
                        {
                            currentSplitDoc.AddPage(pdfDocument.Pages[i]);
                            if (string.IsNullOrEmpty(currentEmail) && !string.IsNullOrEmpty(pageEmail)) currentEmail = pageEmail;
                            if (string.IsNullOrEmpty(currentFileName) && !string.IsNullOrEmpty(pageFileName)) currentFileName = pageFileName;
                            if (string.IsNullOrEmpty(currentType) && !string.IsNullOrEmpty(pageType)) currentType = pageType;
                            if (string.IsNullOrEmpty(currentPassword) && !string.IsNullOrEmpty(pagePassword)) currentPassword = pagePassword;
                            foreach(var kv in pageVariables)
                            {
                                currentVariables[kv.Key] = kv.Value;
                            }
                        }
                    }

                    if (currentSplitDoc != null && !string.IsNullOrEmpty(currentFileName))
                    {
                        SaveAndQueueDocument(currentSplitDoc, stagingDir, currentFileName, currentEmail, currentType, currentPassword, profiles, queueEngine);
                    }
                }
            }

            string destFile = Path.Combine(processedDir, Path.GetFileName(batchPdfPath));
            if (File.Exists(destFile)) File.Delete(destFile);
            File.Move(batchPdfPath, destFile);
        }

        private void SaveAndQueueDocument(PdfSharp.Pdf.PdfDocument doc, string stagingDir, string fileName, string email, string type, string password, List<RptProfile> profiles, QueueEngine queueEngine)
        {
            var profile = profiles.Find(p => p.Id == type);
            if (profile != null && profile.EnableEncryption && !string.IsNullOrEmpty(password))
            {
                doc.SecuritySettings.UserPassword = password;
                doc.SecuritySettings.OwnerPassword = password;
            }

            string outPath = Path.Combine(stagingDir, $"{fileName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf");
            doc.Save(outPath);
            doc.Dispose();

            queueEngine.AddPendingEmail(outPath, type ?? "", email ?? "");
        }
    }
}
