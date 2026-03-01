namespace Api.Common.Security;

/// <summary>
/// Encrypts and decrypts tenant connection strings for storage in the catalogue DB.
/// In Development mode without a configured key, passes through plaintext.
/// </summary>
internal interface IConnectionStringProtector
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}
