using System;
using System.Security.Cryptography;
using System.Text;

namespace ModelessForms.IssuesManager.Storage
{
    /// <summary>Encrypts small secrets (the Azure key) for storage in a settings file.</summary>
    public interface ISecretProtector
    {
        string Protect(string plainText);
        string Unprotect(string protectedText);
    }

    /// <summary>
    /// Windows DPAPI, current-user scope: only the same Windows user on the same
    /// machine can decrypt. A settings file copied to another user or PC therefore
    /// loses the key (the user must enter it again), which is the intended protection.
    /// </summary>
    public sealed class DpapiSecretProtector : ISecretProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Norsyn.IssueTracker.v1");

        public string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(bytes);
        }

        /// <exception cref="CryptographicException">The text was protected by another user or machine.</exception>
        public string Unprotect(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return string.Empty;
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedText), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
