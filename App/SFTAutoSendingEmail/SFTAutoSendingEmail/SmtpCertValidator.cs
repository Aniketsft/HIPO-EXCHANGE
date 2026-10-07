using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace AutoEmailer
{
    public enum CertTrustMode
    {
        /// <summary>Use the Windows trust store (default).</summary>
        System = 0,
        /// <summary>Accept only if a certificate in the server chain matches the pinned SHA-256 thumbprint.</summary>
        Pinned = 1,
        /// <summary>Accept any certificate. Vulnerable to man-in-the-middle attacks.</summary>
        AllowUntrusted = 2
    }

    public class FetchedCertInfo
    {
        public string Subject { get; set; } = "";
        public string Issuer { get; set; } = "";
        public DateTime NotAfter { get; set; }
        public string Thumbprint { get; set; } = "";
        /// <summary>True when the pinned certificate is the top of the chain (CA) rather than the server leaf.</summary>
        public bool IsCa { get; set; }
    }

    public static class SmtpCertValidator
    {
        public static string NormalizeThumbprint(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return new string(value.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        }

        public static string ComputeThumbprint(X509Certificate certificate)
        {
            using var cert2 = new X509Certificate2(certificate);
            return cert2.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
        }

        /// <summary>Core decision logic, kept free of MailKit so it is unit-testable.</summary>
        public static bool IsTrusted(
            CertTrustMode mode,
            string? pinnedThumbprint,
            SslPolicyErrors errors,
            IEnumerable<string> chainThumbprints)
        {
            switch (mode)
            {
                case CertTrustMode.AllowUntrusted:
                    return true;
                case CertTrustMode.Pinned:
                    var pin = NormalizeThumbprint(pinnedThumbprint);
                    if (pin.Length == 0) return false;
                    return chainThumbprints.Any(t => string.Equals(NormalizeThumbprint(t), pin, StringComparison.Ordinal));
                default:
                    return errors == SslPolicyErrors.None;
            }
        }

        public static RemoteCertificateValidationCallback Create(SmtpConfig cfg)
        {
            var mode = cfg.CertTrustMode;
            var pin = cfg.PinnedThumbprint;

            return (sender, certificate, chain, errors) =>
            {
                var thumbs = new List<string>();
                if (certificate != null) thumbs.Add(ComputeThumbprint(certificate));
                if (chain != null)
                {
                    foreach (var element in chain.ChainElements)
                        thumbs.Add(ComputeThumbprint(element.Certificate));
                }
                return IsTrusted(mode, pin, errors, thumbs);
            };
        }

        /// <summary>
        /// Connects without validation purely to read the server certificate chain. Nothing is authenticated or sent.
        /// Prefers the top of the chain (CA) so pinning survives server certificate renewals.
        /// </summary>
        public static FetchedCertInfo FetchCertificate(string host, int port, bool useSsl)
        {
            X509Certificate2? leaf = null;
            X509Certificate2? top = null;

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, cert, chain, errors) =>
            {
                if (cert != null) leaf = new X509Certificate2(cert);
                if (chain != null && chain.ChainElements.Count > 1)
                {
                    top = new X509Certificate2(chain.ChainElements[chain.ChainElements.Count - 1].Certificate);
                }
                return true;
            };

            client.Connect(host, port == 0 ? 587 : port,
                useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            client.Disconnect(true);

            var chosen = top ?? leaf ?? throw new InvalidOperationException("The server did not present a certificate.");
            return new FetchedCertInfo
            {
                Subject = chosen.Subject,
                Issuer = chosen.Issuer,
                NotAfter = chosen.NotAfter,
                Thumbprint = chosen.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256),
                IsCa = top != null
            };
        }
    }
}
