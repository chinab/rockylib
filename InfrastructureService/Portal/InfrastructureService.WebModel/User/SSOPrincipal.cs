using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;
using InfrastructureService.WebModel.SSOService;

namespace InfrastructureService.WebModel
{
    /// <summary>
    /// IPrincipal的实现
    /// </summary>
    public sealed class SSOPrincipal : IPrincipal
    {
        #region NestedTypes
        private class Wrapper : IIdentity
        {
            private SSOIdentity _id;

            public Wrapper(SSOIdentity id)
            {
                _id = id;
            }

            public string AuthenticationType
            {
                get { return "InfrastructureService.SSO"; }
            }

            public bool IsAuthenticated
            {
                get { return _id.IsAuthenticated; }
            }

            public string Name
            {
                get { return _id.UserName; }
            }
        }
        #endregion

        public SSOIdentity Identity { get; private set; }
        IIdentity IPrincipal.Identity
        {
            get { return new Wrapper(this.Identity); }
        }

        public SSOPrincipal(SSOIdentity identity)
        {
            this.Identity = identity;
        }

        public bool IsInRole(string role)
        {
            return false;
        }
    }
}