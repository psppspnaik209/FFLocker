using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace FFLocker.Logic
{
    public static class HelloManager
    {
        private const string FFLockerCredentialName = "FFLockerMasterKey";
        // A static challenge phrase. This must not change, or existing files cannot be unlocked.
        private static readonly IBuffer ChallengeBuffer = CryptographicBuffer.ConvertStringToBinary("FFLockerHelloChallenge", BinaryStringEncoding.Utf8);

        public static async Task<bool> IsHelloSupportedAsync()
        {
            return await KeyCredentialManager.IsSupportedAsync();
        }

        public static async Task<byte[]?> GenerateHelloDerivedKeyWithoutWindowAsync()
        {
            return await GenerateHelloDerivedKeyAsync(IntPtr.Zero);
        }

        /// <summary>
        /// Generates a 32-byte key derived from a Windows Hello signature.
        /// This requires user interaction via the Windows Hello prompt.
        /// </summary>
        /// <param name="hwnd">The window handle for the prompt.</param>
        /// <returns>A 32-byte key, or null if the operation fails or is canceled.</returns>
        public static async Task<byte[]?> GenerateHelloDerivedKeyAsync(IntPtr hwnd)
        {
            KeyCredentialRetrievalResult openResult;
            try
            {
                // First, try to open an existing credential.
                openResult = await KeyCredentialManager.OpenAsync(FFLockerCredentialName);

                // If no credential exists, or if it's unusable, create a new one.
                if (openResult.Status != KeyCredentialStatus.Success)
                {
                    openResult = await KeyCredentialManager.RequestCreateAsync(FFLockerCredentialName, KeyCredentialCreationOption.ReplaceExisting);
                }

                // If we still don't have a credential, we cannot proceed.
                if (openResult.Status != KeyCredentialStatus.Success)
                {
                    return null;
                }
            }
            catch (Exception)
            {
                // This can happen if the user cancels the prompt.
                return null;
            }

            var credential = openResult.Credential;
            
            // Sign the static challenge. This proves the user's identity via Hello.
            var signResult = await credential.RequestSignAsync(ChallengeBuffer);

            if (signResult.Status != KeyCredentialStatus.Success)
            {
                return null;
            }

            // The signature itself is not the key. We derive a key from it using a hash.
            // This ensures we always get a 32-byte key suitable for AES.
            var signature = signResult.Result;
            var hashProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            var hashedSignature = hashProvider.HashData(signature);

            return hashedSignature.ToArray();
        }
    }
}
