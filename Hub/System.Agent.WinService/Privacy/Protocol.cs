using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace System.Agent.Privacy
{
    [Serializable]
    public class PrivacyConfigEntity
    {
        public string Password { get; set; }
        public double Opacity { get; set; }
        public Image Background { get; set; }
        public string Drive { get; set; }
    }

    public enum Cmd
    {
        Lock = 1,
        Format = 2,
    }
}