using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;

namespace System.Agent.Privacy
{
    [Serializable]
    public class ConfigEntity
    {
        public string Password { get; set; }
        public Image Background { get; set; }
        public char Drive { get; set; }
    }
}