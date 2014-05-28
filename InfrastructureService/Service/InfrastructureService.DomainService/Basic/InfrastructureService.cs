using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using InfrastructureService.Contract;
using InfrastructureService.Model.Basic;
using InfrastructureService.Repository.Basic;
using Newtonsoft.Json;
using System.Net.WCF;

namespace InfrastructureService.DomainService
{
    public class InfrastructureService : ServiceBase, IInfrastructureService
    {
        #region NestedTypes
        private class JsonHeader
        {
            public Guid AppID { get; set; }
            public string FileKey { get; set; }
        }
        #endregion

        #region Constructors
        static InfrastructureService()
        {
            int listenPort;
            if (!int.TryParse(ConfigurationManager.AppSettings["StorageServiceListenPort"], out listenPort))
            {
                throw new InvalidOperationException("StorageServiceListenPort");
            }
            _config = new StorageConfig();
            _config.ListenedAddress = new System.Net.IPEndPoint(InfrastructureRepository.LocalIP, listenPort);
            _config.StorageUrl = ConfigurationManager.AppSettings["StorageServiceStorageUrl"];
            if (string.IsNullOrEmpty(_config.StorageUrl))
            {
                throw new InvalidOperationException("StorageServiceStorageUrl");
            }
        }
        #endregion

        #region Message
        public void SendEmail(SendEmailParameter param)
        {
            var repository = new InfrastructureRepository();
            repository.SendEmail(param);
        }

        public void SendSMS(SendSMSParameter param)
        {
            var repository = new InfrastructureRepository();
            repository.SendSMS(param);
        }

        public decimal GetSMSBalance(Guid configID)
        {
            var repository = new InfrastructureRepository();
            return repository.GetSMSBalance(configID);
        }
        #endregion

        #region File
        #region Fields
        private static FileTransfer _transfer;
        private static StorageConfig _config;
        private static readonly string[] ImageExts = new string[] { ".jpg", ".gif", ".png" };
        #endregion

        #region Methods
        /// <summary>
        /// Create FileTransfer
        /// </summary>
        public static void Create(bool dispose = false)
        {
            if (dispose && _transfer != null)
            {
                _transfer.Dispose();
                return;
            }
            _transfer = new FileTransfer();
            _transfer.Listen(InfrastructureRepository.RootPath, (ushort)_config.ListenedAddress.Port);
            _transfer.Prepare += new EventHandler<TransferEventArgs>(_transfer_Prepare);
            _transfer.Completed += new EventHandler<TransferEventArgs>(_transfer_Completed);
        }
        static void _transfer_Completed(object sender, TransferEventArgs e)
        {
            var trans = (FileTransfer)sender;
            var header = JsonConvert.DeserializeObject<JsonHeader>(e.Config.State.ToString());
            string sourceFilePath = string.Format(@"{0}{1}\{2}{3}", InfrastructureRepository.RootPath, trans.DirectoryPath, header.FileKey, Path.GetExtension(e.Config.FileName));

            if (header.FileKey != CryptoManaged.MD5HashFile(sourceFilePath).ToString())
            {
                File.Delete(sourceFilePath);
                return;
            }
            var repository = new InfrastructureRepository();
            repository.SaveFile(header.FileKey, e.Config.FileName, sourceFilePath);
        }
        static void _transfer_Prepare(object sender, TransferEventArgs e)
        {
            var trans = (FileTransfer)sender;
            if (e.Config.State == null)
            {
                throw new InvalidOperationException("Config.State");
            }

            var header = JsonConvert.DeserializeObject<JsonHeader>(e.Config.State.ToString());
            trans.DirectoryPath = header.AppID.ToString("N");
            var repository = new InfrastructureRepository();
            e.Cancel = repository.ExistFile(new QueryFileParameter()
            {
                AppID = header.AppID,
                FileKey = header.FileKey
            });
        }

        private static bool TryGetPath(SaveFileParameter param, out Guid checksum, out string path)
        {
            checksum = CryptoManaged.MD5Hash(new MemoryStream(param.FileData));
            path = string.Format(@"{0}{1}\{2}{3}", InfrastructureRepository.RootPath, param.AppID.ToString("N"), checksum, Path.GetExtension(param.FileName));
            var file = new FileInfo(path);
            FileStream x = null;
            try
            {
                return !(file.Exists && CryptoManaged.MD5Hash(x = file.OpenRead()) == checksum);
            }
            finally
            {
                if (x != null)
                {
                    x.Close();
                }
            }
        }
        #endregion

        #region WCF Methods
        public StorageConfig GetConfig()
        {
            return _config;
        }

        public void SaveFile(SaveFileParameter param)
        {
            Guid checksum;
            string path;
            if (!TryGetPath(param, out checksum, out path))
            {
                return;
            }
            File.WriteAllBytes(path, param.FileData);
            var repository = new InfrastructureRepository();
            repository.SaveFile(checksum.ToString(), param.FileName, path);
        }

        public QueryFileResult QueryFile(QueryFileParameter param)
        {
            var repository = new InfrastructureRepository();
            return repository.QueryFile(param);
        }

        public string GetFileUrl(GetFileUrlParameter param)
        {
            var repository = new InfrastructureRepository();
            var pathObj = repository.QueryFilePath(new QueryFileParameter()
            {
                AppID = param.AppID,
                FileKey = param.FileKey
            });
            string outFilePath = pathObj.VirtualPath, fileExt;
            var size = param.ImageThumbnailSize;
            if (size.HasValue && ImageExts.Contains(fileExt = Path.GetExtension(pathObj.PhysicalPath)))
            {
                string filePath = Path.ChangeExtension(pathObj.PhysicalPath, string.Format("{0},{1}", size.Value.Width, size.Value.Height) + fileExt);
                try
                {
                    if (!File.Exists(filePath))
                    {
                        GDIHelper.MakeThumbnailImage(pathObj.PhysicalPath, filePath, size.Value.Width, size.Value.Height, ThumbnailMode.Zoom);
                    }
                    outFilePath = outFilePath.Replace(Path.GetFileName(pathObj.PhysicalPath), Path.GetFileName(filePath));
                }
                catch (Exception ex)
                {
                    App.LogError(ex, "GetFileUrl");
#if DEBUG
                    throw;
#endif
                }
            }
            return _config.StorageUrl + outFilePath;
        }
        #endregion
        #endregion
    }
}