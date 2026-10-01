using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AutoEmailer
{
    public class SmtpConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
    }

    public class EmailSender
    {
        private readonly List<RptProfile> _profiles;

        public EmailSender(List<RptProfile> profiles)
        {
            _profiles = profiles;
        }

        public async Task SendQueueEmailsAsync(List<PendingEmail> emails)
        {
            var groupedEmails = emails
                .Where(e => !string.IsNullOrEmpty(e.EmailAddress))
                .GroupBy(e => new { e.EmailAddress, e.ProfileId })
                .ToList();

            foreach (var group in groupedEmails)
            {
                var profile = _profiles.FirstOrDefault(p => p.Id == group.Key.ProfileId);
                if (profile == null)
                {
                    profile = _profiles.FirstOrDefault();
                    if (profile == null) continue;
                }

                var config = profile.SmtpConfig;
                
                using (var client = new SmtpClient())
                {
                    try
                    {
                        await client.ConnectAsync(config.Host, config.Port, config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                        if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
                        {
                            await client.AuthenticateAsync(config.Username, config.Password);
                        }

                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(config.FromName, config.FromAddress));

                        var recipients = group.Key.EmailAddress.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var recipient in recipients)
                        {
                            message.To.Add(new MailboxAddress("", recipient.Trim()));
                        }

                        if (!string.IsNullOrEmpty(profile.CC))
                        {
                            var ccs = profile.CC.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var cc in ccs)
                            {
                                message.Cc.Add(new MailboxAddress("", cc.Trim()));
                            }
                        }

                        if (!string.IsNullOrEmpty(profile.BCC))
                        {
                            var bccs = profile.BCC.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var bcc in bccs)
                            {
                                message.Bcc.Add(new MailboxAddress("", bcc.Trim()));
                            }
                        }

                        message.Subject = "Your Documents from Sage X3";

                        var builder = new BodyBuilder();
                        
                        string body = profile.HtmlTemplate.Replace("{CustomerEmail}", group.Key.EmailAddress);

                        builder.HtmlBody = body;

                        foreach (var doc in group)
                        {
                            if (File.Exists(doc.FilePath))
                            {
                                builder.Attachments.Add(doc.FilePath);
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(profile.StaticAttachmentPath) && File.Exists(profile.StaticAttachmentPath))
                        {
                            builder.Attachments.Add(profile.StaticAttachmentPath);
                        }

                        message.Body = builder.ToMessageBody();

                        await client.SendAsync(message);
                        await client.DisconnectAsync(true);
                        
                        HipodocLogger.Info(profile.Id, $"Successfully sent email to {group.Key.EmailAddress} with {group.Count()} attachments.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email: {ex.Message}");
                        HipodocLogger.Error(profile.Id, $"Error sending email to {group.Key.EmailAddress}: {ex.Message}");
                        throw;
                    }
                }
            }
        }
    }
}
