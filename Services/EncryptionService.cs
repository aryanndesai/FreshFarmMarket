using System;
using Microsoft.AspNetCore.DataProtection;

namespace FreshFarmMarket.Services
{
    public class EncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("FreshFarmMarket.CreditCardProtection");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            return _protector.Protect(plainText);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                return _protector.Unprotect(cipherText);
            }
            catch
            {
                return "****DECRYPTION ERROR****";
            }
        }
    }
}