using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoEmailer
{
    public static class LicenseValidator
    {
        public static void Validate()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string publicKeyFile = Path.Combine(basePath, "rsa_public.pem");
            string licenseFile = Path.Combine(basePath, "license.key");

            if (!File.Exists(publicKeyFile))
            {
                throw new Exception("License public key (rsa_public.pem) not found. Application cannot start.");
            }

            if (!File.Exists(licenseFile))
            {
                throw new Exception("License token file (license.key) not found. Application cannot start.");
            }

            string token = File.ReadAllText(licenseFile).Trim();
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                throw new Exception("Invalid license format. Expected base64Payload.base64Signature");
            }

            string payloadBase64 = parts[0];
            string signatureBase64 = parts[1];

            // Verify Signature
            string publicKeyPem = File.ReadAllText(publicKeyFile);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            byte[] rawPayloadBytes = Convert.FromBase64String(payloadBase64);
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

            bool isValid = rsa.VerifyData(rawPayloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!isValid)
            {
                throw new Exception("License signature is invalid or corrupted.");
            }

            // Verify Expiry
            string payloadJson = Encoding.UTF8.GetString(rawPayloadBytes);
            using var doc = JsonDocument.Parse(payloadJson);
            
            if (doc.RootElement.TryGetProperty("expiryDate", out JsonElement expiryElement))
            {
                if (DateTime.TryParse(expiryElement.GetString(), out DateTime expiryDate))
                {
                    if (DateTime.UtcNow > expiryDate)
                    {
                        throw new Exception($"License expired on {expiryDate:yyyy-MM-dd}.");
                    }
                }
                else
                {
                    throw new Exception("Invalid expiry date format in license.");
                }
            }
            else
            {
                throw new Exception("License does not contain an expiry date.");
            }
            
            // License is perfectly valid and not expired!
        }
    }
}
