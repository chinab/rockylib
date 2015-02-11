using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    [Serializable]
    public class AjaxBlockEntity
    {
        internal const string AjaxBlock = "_AjaxBlock";
        public string ID { get; set; }
        public string Text { get; set; }
        public string SuccessScript { get; set; }
        public AjaxBlockFlags Flags { get; set; }
    }

    [Flags]
    public enum AjaxBlockFlags
    {
        Block = 1 << 0,
        Event = 1 << 1,
        All = Block | Event
    }
}