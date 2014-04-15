using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace InfrastructureService.Repository.Basic
{
    /// <summary>
    /// Author: WangXiaoming
    ///   Date: 2012/11/29 13:44:31
    /// </summary>
    public class MessageConfig
    {
        public static readonly MessageConfig Default;

        static MessageConfig()
        {
            Default = new MessageConfig();
            Default.LogEmail = bool.TrueString.Equals(ConfigurationManager.AppSettings["LogEmail"], StringComparison.OrdinalIgnoreCase);
            Default.LogSMS = bool.TrueString.Equals(ConfigurationManager.AppSettings["LogSMS"], StringComparison.OrdinalIgnoreCase);
            Default.ResendFailEmail = bool.TrueString.Equals(ConfigurationManager.AppSettings["ResendFailEmail"], StringComparison.OrdinalIgnoreCase);
            Guid resendFailEmailConfigID;
            if (Guid.TryParse(ConfigurationManager.AppSettings["ResendFailEmailConfigID"], out resendFailEmailConfigID))
            {
                Default.ResendFailEmailConfigID = resendFailEmailConfigID;
            }
        }

        public bool LogEmail { get; set; }
        public bool LogSMS { get; set; }
        public bool ResendFailEmail { get; set; }
        public Guid? ResendFailEmailConfigID { get; set; }
    }
}