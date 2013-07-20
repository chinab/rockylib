using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace System
{
    public sealed class CryptoManaged : Disposable
    {
        #region StaticMembers
        public static string NewSalt
        {
            get
            {
                byte[] data = new byte[32];
                new Random().NextBytes(data);
                return MD5Hex(new MemoryStream(data));
            }
        }

        public static string MD5Hex(string input)
        {
            Contract.Requires(input != null);

            byte[] data = Encoding.UTF8.GetBytes(input);
            return MD5Hex(new MemoryStream(data));
        }
        public static string MD5Hex(Stream input)
        {
            Contract.Requires(input != null);

            var sb = new StringBuilder(32);
            using (var hasher = new MD5CryptoServiceProvider())
            {
                byte[] val = hasher.ComputeHash(input);
                for (int i = 0; i < val.Length; i++)
                {
                    sb.Append(val[i].ToString("x2"));
                }
            }
            return sb.ToString();
        }

        public static Guid MD5Hash(string input)
        {
            Contract.Requires(input != null);

            byte[] data = Encoding.UTF8.GetBytes(input);
            return MD5Hash(new MemoryStream(data));
        }
        public static Guid MD5Hash(Stream input)
        {
            Contract.Requires(input != null);

            using (var hasher = new MD5CryptoServiceProvider())
            {
                byte[] val = hasher.ComputeHash(input);
                return new Guid(val);
            }
        }
        public static Guid MD5HashFile(string filePath)
        {
            Contract.Requires(filePath != null);

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return MD5Hash(stream);
            }
        }

        public static void TrustCert(Stream cert, string pwd = null)
        {
            Contract.Requires(cert != null);

            byte[] raw = new byte[cert.Length];
            cert.Read(raw, 0, raw.Length);
            var certificate = new X509Certificate2(raw, pwd);
            var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
        }
        #endregion

        #region Fields
        private string _key, _IV;
        private byte[] _legalKey, _legalIV;
        private RijndaelManaged _rijndael;
        #endregion

        #region Properties
        public string Key
        {
            set
            {
                Contract.Requires(value != null);
                base.CheckDisposed();

                _key = value;
                _rijndael.GenerateKey();
                if (_key.Length > _rijndael.Key.Length)
                {
                    _key = _key.Substring(0, _rijndael.Key.Length);
                }
                else
                {
                    _key = _key.PadRight(_rijndael.Key.Length, ' ');
                }
                _legalKey = Encoding.ASCII.GetBytes(_key);
            }
            get { return _key; }
        }
        public string IV
        {
            set
            {
                Contract.Requires(value != null);
                base.CheckDisposed();

                _IV = value;
                _rijndael.GenerateIV();
                if (_IV.Length > _rijndael.IV.Length)
                {
                    _IV = _IV.Substring(0, _rijndael.IV.Length);
                }
                else
                {
                    _IV = _IV.PadRight(_rijndael.IV.Length, ' ');
                }
                _legalIV = Encoding.ASCII.GetBytes(_IV);
            }
            get { return _IV; }
        }
        #endregion

        #region Constructors
        public CryptoManaged(string key, string iv = null)
        {
            if (iv == null)
            {
                iv = CryptoManaged.NewSalt;
            }

            _rijndael = new RijndaelManaged();
            this.Key = key;
            this.IV = iv;
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                _rijndael.Clear();
            }
            _legalKey = _legalIV = null;
            _rijndael = null;
        }
        #endregion

        #region Methods
        public string Encrypt(string input)
        {
            Contract.Requires(input != null);
            base.CheckDisposed();

            byte[] data = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(Encrypt(new MemoryStream(data)).ToArray());
        }
        public MemoryStream Encrypt(Stream input)
        {
            Contract.Requires(input != null);
            base.CheckDisposed();

            var output = new MemoryStream();
            var crypto = new CryptoStream(input, _rijndael.CreateEncryptor(_legalKey, _legalIV), CryptoStreamMode.Read);
            crypto.FixedCopyTo(output);
            if (!crypto.HasFlushedFinalBlock)
            {
                crypto.FlushFinalBlock();
            }
            output.Position = 0L;
            return output;
        }

        public string Decrypt(string input)
        {
            Contract.Requires(input != null);
            base.CheckDisposed();

            byte[] data = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(Decrypt(new MemoryStream(data)).ToArray());
        }
        public MemoryStream Decrypt(Stream input)
        {
            Contract.Requires(input != null);
            base.CheckDisposed();

            var output = new MemoryStream();
            var crypto = new CryptoStream(input, _rijndael.CreateDecryptor(_legalKey, _legalIV), CryptoStreamMode.Read);
            crypto.FixedCopyTo(output);
            if (!crypto.HasFlushedFinalBlock)
            {
                crypto.FlushFinalBlock();
            }
            output.Position = 0L;
            return output;
        }

        public void EncryptTo(Stream inStream, Stream outStream)
        {
            Contract.Requires(inStream != null && outStream != null);
            base.CheckDisposed();

            var crypto = new CryptoStream(inStream, _rijndael.CreateEncryptor(_legalKey, _legalIV), CryptoStreamMode.Read);
            crypto.FixedCopyTo(outStream);
            if (!crypto.HasFlushedFinalBlock)
            {
                crypto.FlushFinalBlock();
            }
        }
        public void DecryptTo(Stream inStream, Stream outStream)
        {
            Contract.Requires(inStream != null && outStream != null);
            base.CheckDisposed();

            var crypto = new CryptoStream(inStream, _rijndael.CreateDecryptor(_legalKey, _legalIV), CryptoStreamMode.Read);
            crypto.FixedCopyTo(outStream);
            if (!crypto.HasFlushedFinalBlock)
            {
                crypto.FlushFinalBlock();
            }
        }
        #endregion

        #region File
        public void EncryptFile(string inFileName, string outFileName, long splitSize, SplitFileMode mode)
        {
            Contract.Requires(splitSize > 0L);
            base.CheckDisposed();

            FileStream inFileStream = new FileStream(inFileName, FileMode.Open, FileAccess.Read);
            long fileLength = inFileStream.Length, oddSize, avgSize = Math.DivRem(fileLength, splitSize, out oddSize),
                i = 0L, count, size;
            switch (mode)
            {
                case SplitFileMode.InputFileLength:
                    count = avgSize;
                    size = splitSize;
                    break;
                default:
                    count = splitSize;
                    size = avgSize;
                    break;
            }
            using (ICryptoTransform encrypto = _rijndael.CreateEncryptor(_legalKey, _legalIV))
            using (CryptoStream inStream = new CryptoStream(inFileStream, encrypto, CryptoStreamMode.Read))
            {
                while (i < count)
                {
                    long length = (++i) == count ? size + oddSize : size;
                    using (FileStream outStream = new FileStream(String.Concat(outFileName, "_Part", i, ".temp"), FileMode.Create, FileAccess.Write))
                    {
                        inStream.FixedCopyTo(outStream, length);
                        if (i == count)
                        {
                            inStream.FixedCopyTo(outStream);
                            using (var writer = new BinaryWriter(outStream))
                            {
                                writer.Write(fileLength);
                            }
                        }
                    }
                }
            }
        }

        public void DecryptFile(string inFileName, string outFileName, long splitSize, SplitFileMode mode)
        {
            Contract.Requires(splitSize > 0L);
            base.CheckDisposed();

            long i = 1, fileLength;
            while (File.Exists(String.Concat(inFileName, "_Part", i, ".temp")))
            {
                i++;
            }
            FileStream lastInFileStream = new FileStream(String.Concat(inFileName, "_Part", --i, ".temp"), FileMode.Open, FileAccess.ReadWrite);
            long lastInStreamLength = lastInFileStream.Length - 8;
            lastInFileStream.Position = lastInStreamLength;
            using (var reader = new BinaryReader(lastInFileStream))
            {
                fileLength = reader.ReadInt64();
            }
            lastInFileStream.Position = 0L;
            lastInFileStream.SetLength(lastInStreamLength);
            long oddSize, avgSize = Math.DivRem(fileLength, splitSize, out oddSize),
                count, size;
            i = 0L;
            switch (mode)
            {
                case SplitFileMode.InputFileLength:
                    count = avgSize;
                    size = splitSize;
                    break;
                default:
                    count = splitSize;
                    size = avgSize;
                    break;
            }
            using (ICryptoTransform decrypto = _rijndael.CreateDecryptor(_legalKey, _legalIV))
            using (CryptoStream outStream = new CryptoStream(new FileStream(outFileName, FileMode.Create, FileAccess.Write), decrypto, CryptoStreamMode.Write))
            {
                while (i < count)
                {
                    long length = (++i) == count ? size + oddSize : size;
                    using (FileStream inStream = i == count ? lastInFileStream : new FileStream(String.Concat(inFileName, "_Part", i, ".temp"), FileMode.Open, FileAccess.Read))
                    {
                        inStream.FixedCopyTo(outStream, length);
                        if (i == count)
                        {
                            inStream.FixedCopyTo(outStream);
                        }
                    }
                }
            }
        }
        #endregion
    }
}