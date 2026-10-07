using System.IO;

namespace AutoEmailer
{
    public static class LicenseManager
    {
        public static bool ValidateLicense(string licenseFilePath, out string message)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, licenseFilePath);
            if (File.Exists(fullPath))
            {
                message = "Valid";
                return true;
            }
            else
            {
                message = "License not found. Please upload.";
                return false;
            }
        }
    }
}
