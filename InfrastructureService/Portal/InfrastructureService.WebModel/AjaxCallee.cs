using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfrastructureService.WebModel.SSOService;

namespace InfrastructureService.WebModel
{
    [AjaxPro.AjaxNamespace("ajax")]
    public static class AjaxCallee
    {
        [AjaxPro.AjaxMethod]
        public static void SendValidSMS(string mobile)
        {
            SSOAuthentication.CurrentService.SendAuthMobile(new SendAuthMobileParameter()
            {
                AppID = SSOAuthentication.AppID,
                UserName = mobile,
                Mobile = mobile
            });
        }
    }
}