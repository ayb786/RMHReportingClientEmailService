using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RMHReportingClientEmailService.Repository
{
    public class Encryptor
    {
        //This will be append with actual password to make it more secure.
        private string SpecialKey = "RMH@I#$%^";

        private MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider();
        public byte[] MD5Hash(string value)
        {
            return MD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(value));
        }


        private TripleDESCryptoServiceProvider DES = new TripleDESCryptoServiceProvider();
        public string Encrypt(string stringToEncrypt, string SpecialKey)
        {
            DES.Key = MD5Hash(SpecialKey);
            DES.Mode = CipherMode.ECB;
            byte[] Buffer = ASCIIEncoding.ASCII.GetBytes(stringToEncrypt);
            return Convert.ToBase64String(DES.CreateEncryptor().TransformFinalBlock(Buffer, 0, Buffer.Length));
        }


        public string Decrypt(string encryptedString)
        {
            DES.Key = MD5Hash(SpecialKey);
            DES.Mode = CipherMode.ECB;
            byte[] Buffer = Convert.FromBase64String(encryptedString);
            return ASCIIEncoding.ASCII.GetString(DES.CreateDecryptor().TransformFinalBlock(Buffer, 0, Buffer.Length));

        }
    }
}
