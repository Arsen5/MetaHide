using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MetaHide.model
{
    public class EncryptionModel
    {
        public enum EncryptionType
        {
            None,
            XOR,
            AES128,
            AES256
        }

        public byte[] Encrypt(byte[] data, string password, EncryptionType type)
        {
            if (type == EncryptionType.None || string.IsNullOrEmpty(password))
                return data;

            try
            {
                switch (type)
                {
                    case EncryptionType.XOR:
                        return EncryptXOR(data, password);
                    case EncryptionType.AES128:
                        return EncryptAES(data, password, 128);
                    case EncryptionType.AES256:
                        return EncryptAES(data, password, 256);
                    default:
                        return data;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка шифрования: {ex.Message}");
            }
        }

        public byte[] Decrypt(byte[] encryptedData, string password, EncryptionType type)
        {
            if (type == EncryptionType.None || string.IsNullOrEmpty(password))
                return encryptedData;

            try
            {
                switch (type)
                {
                    case EncryptionType.XOR:
                        return DecryptXOR(encryptedData, password);
                    case EncryptionType.AES128:
                        return DecryptAES(encryptedData, password, 128);
                    case EncryptionType.AES256:
                        return DecryptAES(encryptedData, password, 256);
                    default:
                        return encryptedData;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка дешифрования: {ex.Message}");
            }
        }

        private byte[] EncryptXOR(byte[] data, string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] key = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] result = new byte[data.Length];

                for (int i = 0; i < data.Length; i++)
                {
                    result[i] = (byte)(data[i] ^ key[i % key.Length]);
                }

                return result;
            }
        }

        private byte[] DecryptXOR(byte[] encryptedData, string password)
        {
            return EncryptXOR(encryptedData, password);
        }

        private byte[] EncryptAES(byte[] data, string password, int keySize)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = keySize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var deriveBytes = new Rfc2898DeriveBytes(password,
                    Encoding.UTF8.GetBytes("MetaHideSalt"), 10000))
                {
                    aes.Key = deriveBytes.GetBytes(keySize / 8);
                    aes.GenerateIV();
                }

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                    }

                    return ms.ToArray();
                }
            }
        }

        private byte[] DecryptAES(byte[] encryptedData, string password, int keySize)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = keySize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var deriveBytes = new Rfc2898DeriveBytes(password,
                    Encoding.UTF8.GetBytes("MetaHideSalt"), 10000))
                {
                    aes.Key = deriveBytes.GetBytes(keySize / 8);
                }

                byte[] iv = new byte[aes.IV.Length];
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(
                        new MemoryStream(encryptedData, iv.Length, encryptedData.Length - iv.Length),
                        decryptor, CryptoStreamMode.Read))
                    {
                        cs.CopyTo(ms);
                    }

                    return ms.ToArray();
                }
            }
        }
    }
}