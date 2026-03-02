using System.Security.Cryptography;
using Api.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Security;

public class ConnectionStringProtectorTests
{
    private static string GenerateBase64Key()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static ConnectionStringProtector CreateProtector(string? key = null, bool isDevelopment = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:ConnectionStringKey"] = key ?? ""
            })
            .Build();

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? "Development" : "Production");

        return new ConnectionStringProtector(config, env, NullLogger<ConnectionStringProtector>.Instance);
    }

    [Fact]
    public void Protect_Unprotect_RoundTrip_ReturnsOriginal()
    {
        var key = GenerateBase64Key();
        var protector = CreateProtector(key);

        var original = "Server=myserver;Database=mydb;User Id=sa;Password=secret123;";
        var encrypted = protector.Protect(original);
        var decrypted = protector.Unprotect(encrypted);

        Assert.NotEqual(original, encrypted);
        Assert.StartsWith("ENC:", encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Protect_DifferentCallsProduceDifferentCiphertext()
    {
        var key = GenerateBase64Key();
        var protector = CreateProtector(key);

        var original = "Server=myserver;Database=mydb;";
        var encrypted1 = protector.Protect(original);
        var encrypted2 = protector.Protect(original);

        // Different nonces → different ciphertext
        Assert.NotEqual(encrypted1, encrypted2);

        // Both decrypt to the same value
        Assert.Equal(original, protector.Unprotect(encrypted1));
        Assert.Equal(original, protector.Unprotect(encrypted2));
    }

    [Fact]
    public void Unprotect_PlaintextWithoutPrefix_ReturnedAsIs()
    {
        var key = GenerateBase64Key();
        var protector = CreateProtector(key);

        var plaintext = "Server=myserver;Database=mydb;Trusted_Connection=True;";
        var result = protector.Unprotect(plaintext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Protect_NoKey_DevMode_PassthroughPlaintext()
    {
        var protector = CreateProtector(key: null, isDevelopment: true);

        var original = "Server=myserver;Database=mydb;";
        var result = protector.Protect(original);

        Assert.Equal(original, result);
        Assert.DoesNotContain("ENC:", result);
    }

    [Fact]
    public void Unprotect_NoKey_DevMode_PassthroughPlaintext()
    {
        var protector = CreateProtector(key: null, isDevelopment: true);

        var plaintext = "Server=myserver;Database=mydb;";
        var result = protector.Unprotect(plaintext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Constructor_NoKey_ProductionMode_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CreateProtector(key: null, isDevelopment: false));
    }

    [Fact]
    public void Constructor_InvalidKeyLength_Throws()
    {
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        Assert.Throws<InvalidOperationException>(() =>
            CreateProtector(key: shortKey));
    }

    [Fact]
    public void Protect_EmptyString_ReturnsEmpty()
    {
        var protector = CreateProtector(GenerateBase64Key());

        Assert.Equal("", protector.Protect(""));
        Assert.Equal("", protector.Unprotect(""));
    }

    [Fact]
    public void Unprotect_TamperedCiphertext_ThrowsCryptographicException()
    {
        var key = GenerateBase64Key();
        var protector = CreateProtector(key);

        var encrypted = protector.Protect("Server=myserver;Database=mydb;");

        // Tamper with the ciphertext
        var tampered = encrypted[..^2] + "XX";

        Assert.ThrowsAny<Exception>(() => protector.Unprotect(tampered));
    }

    [Fact]
    public void Unprotect_WrongKey_ThrowsCryptographicException()
    {
        var protector1 = CreateProtector(GenerateBase64Key());
        var protector2 = CreateProtector(GenerateBase64Key());

        var encrypted = protector1.Protect("Server=myserver;Database=mydb;");

        Assert.ThrowsAny<Exception>(() => protector2.Unprotect(encrypted));
    }
}
