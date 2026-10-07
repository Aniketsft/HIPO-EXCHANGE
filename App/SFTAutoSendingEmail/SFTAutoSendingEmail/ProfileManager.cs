using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoEmailer
{
    public class RptProfile
    {
        public string Id { get; set; } = "";
        public SmtpConfig SmtpConfig { get; set; } = new SmtpConfig();
        public string HtmlTemplate { get; set; } = "<h1>Hello,</h1><p>Please find attached your documents.</p>";
        public string CC { get; set; } = "";
        public string BCC { get; set; } = "";
        public string StaticAttachmentPath { get; set; } = "";
        public bool EnableEncryption { get; set; }
    }

    public class ProfileManager
    {
        private readonly string _filePath = "profiles.json";

        public List<RptProfile> LoadProfiles()
        {
            if (!File.Exists(_filePath))
            {
                return new List<RptProfile>();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<RptProfile>>(json) ?? new List<RptProfile>();
        }

        public void SaveProfiles(List<RptProfile> profiles)
        {
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
