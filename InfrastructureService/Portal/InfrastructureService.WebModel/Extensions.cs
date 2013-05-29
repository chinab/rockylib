using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfrastructureService.WebModel.SSOService;

namespace InfrastructureService.WebModel
{
    public static partial class Extensions
    {
        public static void RegisterForAjax(this System.Web.UI.Page page)
        {
            AjaxPro.Utility.RegisterTypeForAjax(typeof(AjaxCallee));
        }

        public static IOAuthHandler CreateOAuthHandler(this System.Web.UI.Page page, OAuthKind kind)
        {
            var type = Type.GetType(string.Format("InfrastructureService.WebModel.{0}Handler", kind.ToString()), true);
            return (IOAuthHandler)Activator.CreateInstance(type);
        }
    }
}