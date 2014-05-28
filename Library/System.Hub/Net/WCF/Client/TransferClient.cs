using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.InfrastructureService;

namespace System.Net.WCF
{
    public sealed class TransferClient
    {
        #region Static
        public static string GetFileUrl(Guid appID, string fileKey, System.Drawing.Size? imageSize)
        {
            using (var svc = new InfrastructureServiceClient())
            {
                return svc.GetFileUrl(new GetFileUrlParameter()
                {
                    AppID = appID,
                    FileKey = fileKey,
                    ImageThumbnailSize = imageSize
                });
            }
        }
        #endregion

        #region Properties
        public Guid AppID { get; private set; }
        #endregion

        #region Constructors
        public TransferClient(Guid appID)
        {
            if (appID == Guid.Empty)
            {
                throw new ArgumentException("appID");
            }

            this.AppID = appID;
        }
        #endregion

        #region Methods
        public Guid Send(string fileName, byte[] fileData)
        {
            using (var svc = new InfrastructureServiceClient())
            {
                svc.Invoke(t => t.SaveFile(new SaveFileParameter()
                {
                    AppID = this.AppID,
                    FileName = fileName,
                    FileData = fileData
                }));
            }
            return CryptoManaged.MD5Hash(new MemoryStream(fileData));
        }

        public Guid SendLarge(string filePath)
        {
            StorageConfig sConfig;
            using (var svc = new InfrastructureServiceClient())
            {
                sConfig = svc.GetConfig();
            }
            var config = new TransferConfig(filePath);
            config.State = this.CreateHeader(config);
            var trans = new FileTransfer();
            trans.Send(config, sConfig.ListenedAddress);
            return config.Checksum;
        }
        private string CreateHeader(TransferConfig config)
        {
            var json = new StringBuilder();
            json.Append("{");
            json.AppendFormat("AppID:\"{0}\",FileKey:\"{1}\"", this.AppID, config.Checksum);
            json.Append("}");
            return json.ToString();
        }
        #endregion
    }
}