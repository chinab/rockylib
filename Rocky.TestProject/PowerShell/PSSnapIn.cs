using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Rocky.TestProject
{
    [RunInstaller(true)]
    public class SocksCmdlets : PSSnapIn
    {
        public override string Name
        {
            get { return "SocksCmdlets"; }
        }
        public override string Vendor
        {
            get { return "JeansMan Studio"; }
        }
        public override string Description
        {
            get { return "Socks Common Cmdlets"; }
        }
    }
}