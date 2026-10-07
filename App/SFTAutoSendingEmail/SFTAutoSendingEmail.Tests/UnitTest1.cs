using System.Net.Security;
using System.Text.Json;
using AutoEmailer;

namespace SFTAutoSendingEmail.Tests;

public class SmtpCertValidatorTests
{
    private static readonly string[] Chain = { "AA:BB:CC", "11-22-33" };

    [Fact]
    public void System_Rejects_ChainErrors()
    {
        Assert.False(SmtpCertValidator.IsTrusted(CertTrustMode.System, "", SslPolicyErrors.RemoteCertificateChainErrors, Chain));
    }

    [Fact]
    public void System_Accepts_NoErrors()
    {
        Assert.True(SmtpCertValidator.IsTrusted(CertTrustMode.System, "", SslPolicyErrors.None, Chain));
    }

    [Fact]
    public void Pinned_Accepts_MatchingThumbprint_ChainErrorsIgnored()
    {
        Assert.True(SmtpCertValidator.IsTrusted(CertTrustMode.Pinned, "aa bb cc", SslPolicyErrors.RemoteCertificateChainErrors, Chain));
    }

    [Fact]
    public void Pinned_Rejects_DifferentThumbprint()
    {
        Assert.False(SmtpCertValidator.IsTrusted(CertTrustMode.Pinned, "DEADBEEF", SslPolicyErrors.None, Chain));
    }

    [Fact]
    public void Pinned_Rejects_EmptyPin()
    {
        Assert.False(SmtpCertValidator.IsTrusted(CertTrustMode.Pinned, "", SslPolicyErrors.None, Chain));
    }

    [Fact]
    public void AllowUntrusted_AcceptsAnything()
    {
        Assert.True(SmtpCertValidator.IsTrusted(CertTrustMode.AllowUntrusted, "", SslPolicyErrors.RemoteCertificateNameMismatch, Chain));
    }

    [Theory]
    [InlineData("aa:bb:cc", "AABBCC")]
    [InlineData(" aa-bb cc ", "AABBCC")]
    [InlineData(null, "")]
    public void Thumbprint_Normalizes(string? input, string expected)
    {
        Assert.Equal(expected, SmtpCertValidator.NormalizeThumbprint(input));
    }

    [Fact]
    public void OldConfigJson_DefaultsToSystem()
    {
        var cfg = JsonSerializer.Deserialize<SmtpConfig>("{\"Host\":\"h\",\"Port\":587,\"UseSsl\":true}")!;
        Assert.Equal(CertTrustMode.System, cfg.CertTrustMode);
        Assert.Equal("", cfg.PinnedThumbprint);
    }
}
