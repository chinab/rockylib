using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

[assembly: WebResource("Rocky.Web.Controls.Validator.Resource.Sbg.jpg", "image/jpeg")]
[assembly: WebResource("Rocky.Web.Controls.Validator.Resource.Mbg.jpg", "image/jpeg")]

namespace System.Web.UI
{
    [ToolboxData("<{0}:ImageValidator runat='server'></{0}:ImageValidator>")]
    public class ImageValidator : Image
    {
        internal const string ImageValidatorKey = "ImageValidator";
        /// <summary>
        /// <add verb="GET" path="ValidateHandler*.ashx" type="Rocky.Web.Controls.ValidateHandler, Rocky.Web"/>
        /// </summary>
        protected const string GenerateURL = "/ValidateHandler{0}.ashx?len={1}&bg={2}";

        private string generatedURL;

        public string ValidID
        {
            get { return base.Page.UniqueID + "_" + base.UniqueID; }
        }
        public int CodeLength
        {
            get { return Convert.ToInt32(ViewState["CodeLength"]); }
            set { ViewState["CodeLength"] = value; }
        }

        public void SetGenerator(IValidateCode generator)
        {
            var Request = base.Page.Request;
            string url = "http://" + Request.Url.Host + ":" + Request.Url.Port + base.Page.ClientScript.GetWebResourceUrl(typeof(ImageValidator), "Rocky.Web.Controls.Validator.Resource.Mbg.jpg");
            SetGenerator(generator, url);
        }
        public void SetGenerator(IValidateCode generator, string bgUrl)
        {
            base.Page.Session[ImageValidatorKey] = generator;
            base.ImageUrl = generatedURL = string.Format(GenerateURL, this.ValidID, this.CodeLength, bgUrl);
        }

        protected override void OnPreRender(EventArgs e)
        {
            if (generatedURL == null)
            {
                this.CodeLength = 4;
                SetGenerator(ValidateGenerator.Generator);
            }
            base.ToolTip = "看不清？点击切换一张。";
            base.Style["cursor"] = "pointer";
            base.Attributes["onclick"] = "this.src='" + generatedURL + "&t='+Math.random();";
            base.OnPreRender(e);
        }

        public bool Validate(string value)
        {
            IValidateCode generator = (IValidateCode)base.Page.Session[ImageValidatorKey];
            if (generator == null)
            {
                throw new NullReferenceException("IValidateCode");
            }
            return generator.Validate(this.ValidID, value);
        }
    }
}