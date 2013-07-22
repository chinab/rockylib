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
        public Image Background { get; set; }
        public char Drive { get; set; }
    }

    public enum Cmd
    {
        Config = 1,
        Lock = 2,
        Format = 3,
    }
}