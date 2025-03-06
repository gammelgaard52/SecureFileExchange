using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SecureFileExchange
{
    public class FileService
    {
        private readonly string storagePath = "./secure-storage";
        private readonly Dictionary<string, List<FileMetadata>> passwordFileMap;
        private byte[] DeriveKeyFromPassword(string password)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("fixed_salt"), 100000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 32 bytes = AES-256 key
            }
        }

        public FileService(string password)
        {
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            passwordFileMap = LoadMetadata(password); // ✅ Load metadata using password
        }


        public object SaveFile(IFormFile file, string password)
        {
            string fileId = Guid.NewGuid().ToString();
            string filePath = Path.Combine(storagePath, fileId);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            if (!passwordFileMap.ContainsKey(password))
                passwordFileMap[password] = new List<FileMetadata>();

            var metadata = new FileMetadata
            {
                FileId = fileId,
                FileName = file.FileName,
                ContentType = file.ContentType
            };

            passwordFileMap[password].Add(metadata);
            SaveMetadata(password); // ✅ Ensure metadata is encrypted
            return new { fileId };
        }


        public List<FileMetadata> ListFiles(string password)
        {
            if (!passwordFileMap.ContainsKey(password))
            {
                return new List<FileMetadata>(); // Return empty list if password not found
            }

            return passwordFileMap[password];
        }

        public (byte[] FileContent, string FileName, string ContentType)? GetFile(string password, string fileId)
        {
            if (!passwordFileMap.ContainsKey(password) || passwordFileMap[password].All(f => f.FileId != fileId))
                return null;

            string filePath = Path.Combine(storagePath, fileId);
            if (!File.Exists(filePath))
                return null;

            var metadata = passwordFileMap[password].First(f => f.FileId == fileId);

            // Read encrypted file (including IV)
            byte[] encryptedData = File.ReadAllBytes(filePath);

            return (encryptedData, metadata.FileName, metadata.ContentType);
        }

        private void SaveMetadata(string password)
        {
            string metadataFilePath = Path.Combine(storagePath, "metadata.json");

            var json = JsonSerializer.Serialize(passwordFileMap);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKeyFromPassword(password);
                aes.GenerateIV(); // ✅ Generate a new IV for each encryption
                byte[] iv = aes.IV; // ✅ Store IV separately

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length); // ✅ Store IV in the beginning of the file

                    using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(jsonBytes, 0, jsonBytes.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                    File.WriteAllBytes(metadataFilePath, ms.ToArray());
                }
            }
        }



        private Dictionary<string, List<FileMetadata>> LoadMetadata(string password)
        {
            string metadataFilePath = Path.Combine(storagePath, "metadata.json");

            if (!File.Exists(metadataFilePath))
            {
                return new();
            }

            byte[] encryptedBytes = File.ReadAllBytes(metadataFilePath);

            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKeyFromPassword(password);

                byte[] iv = new byte[16]; // ✅ IV is the first 16 bytes
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv; // ✅ Use extracted IV

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16)) // ✅ Skip IV bytes
                using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    string decryptedJson = reader.ReadToEnd();
                    return JsonSerializer.Deserialize<Dictionary<string, List<FileMetadata>>>(decryptedJson) ?? new();
                }
            }
        }


    }
}
