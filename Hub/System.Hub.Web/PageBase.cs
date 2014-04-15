using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using System.Data;
using System.Collections.Specialized;

namespace System.Web
{
    public class PageBase : System.Web.UI.Page
    {
        #region Fields
        private static readonly string[] DangerousHtmlTags;
        #endregion

        #region Properties
        public static Page Current
        {
            get
            {
                Page page = HttpContext.Current.CurrentHandler as Page;
                if (page == null)
                {
                    throw new InvalidOperationException("CurrentHandler");
                }
                return page;
            }
        }
        #endregion

        #region Constructor
        static PageBase()
        {
            DangerousHtmlTags = new string[] { "script", "object", "form", "body", "iframe", "style", "link", "meta", "applet", "frame", "frameset", "html", "layer", "ilayer" };
        }
        #endregion

        #region FindControl
        public static Control FindControl(Control container, string id)
        {
            Control control = container.Page.FindControl(id);
            if (control == null)
            {
                if (!(container is INamingContainer) && container.NamingContainer != null)
                {
                    container = container.NamingContainer;
                }
                control = FindControlInternal(container, id, null);
                if (control == null)
                {
                    Dictionary<Control, bool> dictionary = new Dictionary<Control, bool>();
                    dictionary[container] = true;
                    container = container.NamingContainer;
                    while (container != null && control == null)
                    {
                        control = FindControlInternal(container, id, dictionary);
                        dictionary[container] = true;
                        container = container.NamingContainer;
                    }
                }
            }
            return control;
        }
        private static Control FindControlInternal(Control control, string id, Dictionary<Control, bool> dictionary)
        {
            if (control.ID == id)
            {
                return control;
            }
            Control child = control.FindControl(id);
            if (child != null && child.ID == id)
            {
                return child;
            }
            foreach (Control temp in control.Controls)
            {
                if (dictionary == null || !dictionary.ContainsKey(temp))
                {
                    child = FindControlInternal(temp, id, dictionary);
                    if (child != null)
                    {
                        return child;
                    }
                }
            }
            return null;
        }
        #endregion

        #region ModelPost
        #region SetPost
        public static void SetPost(object entity)
        {
            SetPost(entity, Current);
        }
        public static void SetPost(object entity, Control parentControl)
        {
            if (parentControl == null)
            {
                throw new ArgumentNullException("parentControl");
            }
            var list = GetPostPair(entity, parentControl);
            if (list.Count == 0)
            {
                throw new InvalidOperationException("PostPair is empty.");
            }

            Page page = parentControl.Page;
            foreach (PostPair pair in list)
            {
                Control control = page.FindControl(pair.UniqueID);
                if (control == null)
                {
                    throw new InvalidOperationException(string.Format("SetPost:'{0}' isn't exists", pair.UniqueID));
                }

                PropertyAccess property = pair.Property;
                object value = property.GetValue(entity);
                Type propType = property.EntityProperty.PropertyType;
                if (!TypeHelper.IsStringOrValueType(propType))
                {
                    if (value != null)
                    {
                        if (value is System.Collections.IEnumerable)
                        {
                            BindData(control, value);
                        }
                        else
                        {
                            SetPost(value, parentControl);
                        }
                    }
                    continue;
                }

                Type underlyingType;
                if (TypeHelper.IsNullableType(propType, out underlyingType))
                {
                    propType = underlyingType;
                }
                if (propType.IsEnum)
                {
                    if (value == null)
                    {
                        continue;
                    }

                    value = Convert.ChangeType(value, typeof(int));

                    ListControl listControl = control as ListControl;
                    if (listControl != null)
                    {
                        if (listControl.Items.Count == 0)
                        {
                            BindEnum(listControl, propType);
                        }
                        if ((listControl is CheckBoxList) && Attribute.IsDefined(propType, typeof(FlagsAttribute)))
                        {
                            int flag = (int)value;
                            foreach (ListItem item in listControl.Items)
                            {
                                int itemValue = int.Parse(item.Value);
                                if ((flag & itemValue) == itemValue)
                                {
                                    item.Selected = true;
                                }
                            }
                            continue;
                        }
                    }
                }
                if (value == null)
                {
                    value = string.Empty;
                }
                ITextControl textControl = control as ITextControl;
                if (textControl != null)
                {
                    textControl.Text = value.ToString();
                }
                else
                {
                    ICheckBoxControl checkBoxControl = control as ICheckBoxControl;
                    if (checkBoxControl != null)
                    {
                        checkBoxControl.Checked = Convert.ToBoolean(value);
                    }
                    else
                    {
                        HiddenField hiddenField = control as HiddenField;
                        if (hiddenField != null)
                        {
                            hiddenField.Value = value.ToString();
                        }
                    }
                }
            }
        }

        private static void BindData(Control ctrl, object source)
        {
            // ListControl & GridView
            BaseDataBoundControl bc = ctrl as BaseDataBoundControl;
            if (bc != null)
            {
                bc.DataSource = source;
                bc.DataBind();
                return;
            }

            Repeater r = ctrl as Repeater;
            if (r != null)
            {
                r.DataSource = source;
                r.DataBind();
                return;
            }

            DataList dl = ctrl as DataList;
            if (dl != null)
            {
                dl.DataSource = source;
                dl.DataBind();
                return;
            }

            throw new NotSupportedException(ctrl.GetType().Name);
        }

        public static void BindEnum(ListControl ctrl, Type enumType)
        {
            foreach (var field in GetEnumField(enumType, ctrl))
            {
                ctrl.Items.Add(new ListItem(field.Description, field.Value.ToString()));
            }
        }
        #endregion

        #region GetPost
        public static void GetPost(object entity)
        {
            GetPost(entity, Current);
        }
        public static void GetPost(object entity, Control parentControl)
        {
            if (parentControl == null)
            {
                throw new ArgumentNullException("parentControl");
            }
            var list = GetPostPair(entity, parentControl);
            if (list.Count == 0)
            {
                throw new InvalidOperationException("PostPair is empty.");
            }

            Page page = parentControl.Page;
            foreach (PostPair pair in list)
            {
                Control control = page.FindControl(pair.UniqueID);
                if (control == null)
                {
                    throw new InvalidOperationException(string.Format("GetPost:'{0}' isn't exists. Remark:如果此ID是OutputCache用户控件，那么在回发的时候可能是Null。", pair.UniqueID));
                }

                WebControl webControl = control as WebControl;
                if (webControl != null && !webControl.Enabled)
                {
                    continue;
                }
                PropertyAccess property = pair.Property;
                Type propType = property.EntityProperty.PropertyType;
                if (!TypeHelper.IsStringOrValueType(propType))
                {
                    continue;
                }

                object value = null;
                Type underlyingType;
                if (TypeHelper.IsNullableType(propType, out underlyingType))
                {
                    propType = underlyingType;
                }
                CheckBoxList listControl;
                if (propType.IsEnum
                    && (listControl = control as CheckBoxList) != null && Attribute.IsDefined(propType, typeof(FlagsAttribute)))
                {
                    int flag = 0;
                    foreach (ListItem item in listControl.Items)
                    {
                        if (item.Selected)
                        {
                            int itemValue = int.Parse(item.Value);
                            flag |= itemValue;
                        }
                    }
                    value = flag;
                }
                else
                {
                    ITextControl textControl = control as ITextControl;
                    if (textControl != null)
                    {
                        value = textControl.Text;
                    }
                    else
                    {
                        ICheckBoxControl checkBoxControl = control as ICheckBoxControl;
                        if (checkBoxControl != null)
                        {
                            value = checkBoxControl.Checked;
                        }
                        else
                        {
                            HiddenField hiddenField = control as HiddenField;
                            if (hiddenField != null)
                            {
                                value = hiddenField.Value;
                            }
                        }
                    }
                }

                if (object.Equals(value, string.Empty))
                {
                    if (!property.IsNullable)
                    {
                        throw new InvalidCastException(property.MappedName);
                    }
                    property.SetValue(entity, null);
                }
                else
                {
                    if (property.EntityProperty.PropertyType == typeof(Guid))
                    {
                        property.SetValue(entity, new Guid(value.ToString()));
                    }
                    else
                    {
                        // property.ChangeType 处理了类型为Enum的情况
                        property.SetValue(entity, property.ChangeType(value));
                    }
                }
            }
        }
        #endregion

        internal class PostPair
        {
            public string UniqueID { get; private set; }
            public PropertyAccess Property { get; private set; }

            public PostPair(string uniqueID, PropertyAccess property)
            {
                this.UniqueID = uniqueID;
                this.Property = property;
            }
        }
        internal class EnumField
        {
            public string Description { get; private set; }
            public int Value { get; private set; }

            public EnumField(string description, int value)
            {
                this.Description = description;
                this.Value = value;
            }
        }

        internal const string ControlPrefix = "_";

        internal static List<PostPair> GetPostPair(object entity, Control parentControl)
        {
            Type entityType = entity.GetType();
            string key = parentControl.Page.Request.Path + entityType.Name;
            List<PostPair> list = (List<PostPair>)HttpRuntime.Cache[key];
            if (list == null)
            {
                lock (GetLocker(parentControl.Page.Request.Path))
                {
                    list = (List<PostPair>)HttpRuntime.Cache[key];
                    if (list == null)
                    {
                        HttpRuntime.Cache[key] = list = new List<PostPair>();
                        foreach (PropertyAccess property in EntityUtility.GetEnumerable(entityType))
                        {
                            Control control = FindControl(parentControl, string.Concat(ControlPrefix, property.MappedName))
                                ?? FindControl(parentControl, string.Concat("auto", property.MappedName));
                            if (control != null)
                            {
                                list.Add(new PostPair(control.UniqueID, property));
                            }
                        }
                    }
                }
            }
            return list;
        }

        internal static List<EnumField> GetEnumField(Type enumType, ListControl control)
        {
            string key = control.Page.Request.Path + enumType.Name;
            List<EnumField> list = (List<EnumField>)HttpRuntime.Cache[key];
            if (list == null)
            {
                lock (GetLocker(control.Page.Request.Path))
                {
                    list = (List<EnumField>)HttpRuntime.Cache[key];
                    if (list == null)
                    {
                        HttpRuntime.Cache[key] = list = new List<EnumField>();
                        Array values = Enum.GetValues(enumType);
                        int i, c;
                        if (Attribute.IsDefined(enumType, typeof(FlagsAttribute)))
                        {
                            i = 1;
                            c = values.Length - 1;
                        }
                        else
                        {
                            i = 0;
                            c = values.Length;
                        }
                        for (; i < c; i++)
                        {
                            object value = values.GetValue(i);
                            list.Add(new EnumField(((Enum)value).ToDescription(), (int)value));
                        }
                    }
                }
            }
            return list;
        }

        #region Locker
        private static System.Collections.Hashtable keyLocker;

        private static object GetLocker(object key)
        {
            if (keyLocker == null)
            {
                System.Threading.Interlocked.CompareExchange(ref keyLocker, new System.Collections.Hashtable(), null);
            }
            object locker = keyLocker[key];
            if (locker == null)
            {
                lock (keyLocker.SyncRoot)
                {
                    locker = keyLocker[key];
                    if (locker == null)
                    {
                        keyLocker[key] = locker = new object();
                    }
                }
            }
            return locker;
        }
        #endregion
        #endregion

        #region Fake MVVM
        public virtual void SetModel<T>(T view, bool asState = false) where T : class
        {
            SetModel(view, asState ? vo =>
            {
#if Session
                var dict = (ListDictionary)Session["ModelState"];
                if (dict == null)
                {
                    Session["ModelState"] = dict = new ListDictionary();
                }
                dict[vo.GetType()] = vo;
#else
                var dict = (ListDictionary)ViewState["ModelState"];
                if (dict == null)
                {
                    ViewState["ModelState"] = dict = new ListDictionary();
                }
                dict[vo.GetType()] = vo;
#endif
            } : (Action<T>)null);
        }
        public virtual void SetModel<T>(T view, Action<T> stateMapper) where T : class
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            PageBase.SetPost(view, this);
            if (stateMapper != null)
            {
                stateMapper(view);
            }
        }

        public virtual T GetModel<T>(bool asState = false) where T : class, new()
        {
            return GetModel<T>(asState ? () =>
            {
#if Session
                var dict = (ListDictionary)Session["ModelState"];
                return dict != null ? ((T)dict[typeof(T)] ?? new T()) : new T();
#else
                var dict = (ListDictionary)ViewState["ModelState"];
                return dict != null ? ((T)dict[typeof(T)] ?? new T()) : new T();
#endif
            } : (Func<T>)null);
        }
        public virtual T GetModel<T>(Func<T> stateMapper) where T : class, new()
        {
            T view;
            if (stateMapper != null)
            {
                view = stateMapper();
                if (view == null)
                {
                    throw new ArgumentException("stateMapper");
                }
            }
            else
            {
                view = new T();
            }
            PageBase.GetPost(view, this);
            return view;
        }
        #endregion

        #region Methods
        public static void RegisterHeader(string keywords, string description)
        {
            Page page = Current;
            page.MetaKeywords += keywords;
            page.MetaDescription += description;
        }
        public static void RegisterCssInclude(string url)
        {
            var context = HttpContext.Current;
            object flag = context.Items[url];
            if (flag == null)
            {
                HtmlLink link = new HtmlLink();
                link.Attributes["type"] = "text/css";
                link.Attributes["rel"] = "stylesheet";
                link.Attributes["href"] = url;
                Current.Header.Controls.Add(link);
                context.Items[url] = string.Empty;
            }
        }
        public static void RegisterScriptInclude(string url)
        {
            var context = HttpContext.Current;
            object flag = context.Items[url];
            if (flag == null)
            {
                HtmlGenericControl script = new HtmlGenericControl("script");
                script.Attributes["type"] = "text/javascript";
                script.Attributes["src"] = url;
                Current.Header.Controls.Add(script);
                context.Items[url] = string.Empty;
            }
        }
        public static void RegisterScript(string script)
        {
            Page page = Current;
            Type type = page.GetType();
            string key = page.ClientScript.IsStartupScriptRegistered(type, string.Empty) ? Guid.NewGuid().ToString() : string.Empty;
            page.ClientScript.RegisterStartupScript(type, key, script, true);
        }

        public static string RenderTitle(object html)
        {
            if (html == null)
            {
                return string.Empty;
            }
            return new StringBuilder(HtmlFilter(html.ToString()))
                .Replace(Environment.NewLine, String.Empty)
                .ToString();
        }
        public static string RenderTitle(object html, int len)
        {
            return RenderTitle(html, len, String.Empty);
        }
        public static string RenderTitle(object html, int len, string extra)
        {
            return StringHelper.ByteSubstring(RenderTitle(html), len, extra);
        }

        public static string HtmlFilter(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }
            return Regex.Replace(html, "<(.[^>]*)>", String.Empty, RegexOptions.Compiled);
        }
        public static string HtmlFilter(string html, string[] tagName)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }
            for (int i = 0; i < tagName.Length; i++)
            {
                switch (tagName[i].ToLower())
                {
                    // 去除所有客户端脚本(但不清除onerror,onmouse)javascipt,vbscript,jscript,js,vbs,event,...
                    case "scriptex":
                        html = Regex.Replace(html, "</?script[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "(javascript|jscript|vbscript|vbs):", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "on(exit|click|key|load)", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "&#", String.Empty, RegexOptions.IgnoreCase);
                        break;
                    // 去除所有客户端脚本javascipt,vbscript,jscript,js,vbs,event,...
                    case "script":
                        html = Regex.Replace(html, "</?script[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "(javascript|jscript|vbscript|vbs):", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "on(mouse|exit|error|click|key|load)", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "&#", String.Empty, RegexOptions.IgnoreCase);
                        break;
                    //去除Object对象，比如flash
                    case "object":
                        html = Regex.Replace(html, "</?object[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "</?param[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "</?embed[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        break;
                    //去除表格<table><tr><td><th>
                    case "table":
                        html = Regex.Replace(html, "</?table[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "</?tbody[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "</?tr[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "</?th[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "</?td[^>]*>", String.Empty, RegexOptions.IgnoreCase);
                        break;
                    default:
                        html = Regex.Replace(html, string.Format("</?{0}[^>]*>", tagName[i]), String.Empty, RegexOptions.IgnoreCase);
                        break;
                }
            }
            return html;
        }

        public static void ResponseScript(string script)
        {
            HttpContext context = HttpContext.Current;
            context.Response.Write("<script>" + script + "</script>");
            context.Response.End();
        }
        public static void AlertThenReload(string msg)
        {
            AlertThenReload(msg, false);
        }
        public static void AlertThenReload(string msg, bool onlyPath)
        {
            HttpContext context = HttpContext.Current;
            if (onlyPath)
            {
                context.Response.Write("<script>alert('" + msg + "');location.href=location.pathname;</script>");
            }
            else
            {
                context.Response.Write("<script>alert('" + msg + "');location.href=location.href;</script>");
            }
            context.Response.End();
        }
        #endregion
    }
}