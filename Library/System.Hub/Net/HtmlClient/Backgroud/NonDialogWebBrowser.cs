using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System.Net
{
    public class NonDialogWebBrowser : WebBrowser
    {
        #region ExtendedWebBrowserSite
        private class ExtendedWebBrowserSite : WebBrowser.WebBrowserSite, UnsafeNativeMethods.IDocHostShowUI
        {
            public ExtendedWebBrowserSite(WebBrowser host)
                : base(host)
            {

            }

            void UnsafeNativeMethods.IDocHostShowUI.ShowMessage(ref UnsafeNativeMethods._RemotableHandle hwnd, string lpstrText, string lpstrCaption, uint dwType, string lpstrHelpFile, uint dwHelpContext, out int plResult)
            {
                plResult = 0;
                //TODO:自定义 
            }
            void UnsafeNativeMethods.IDocHostShowUI.ShowHelp(ref UnsafeNativeMethods._RemotableHandle hwnd, string pszHelpFile, uint uCommand, uint dwData, UnsafeNativeMethods.tagPOINT ptMouse, object pDispatchObjectHit)
            {
                //TODO:自定义 
            }
        }

        protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
        {
            return new ExtendedWebBrowserSite(this);
        }
        #endregion
    }

    internal class UnsafeNativeMethods
    {
        #region IDocHostShowUI
        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        public struct __MIDL_IWinTypes_0009
        {
            // Fields 
            [FieldOffset(0)]
            public int hInproc;
            [FieldOffset(0)]
            public int hRemote;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct _RemotableHandle
        {
            public int fContext;
            public __MIDL_IWinTypes_0009 u;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct tagPOINT
        {
            public int x;
            public int y;
        }

        [ComImport, Guid("C4D244B0-D43E-11CF-893B-00AA00BDCE1A"), InterfaceType((short)1)]
        public interface IDocHostShowUI
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ShowMessage([In, ComAliasName("ExtendedWebBrowser2.UnsafeNativeMethods.wireHWND")] ref _RemotableHandle hwnd, [In, MarshalAs(UnmanagedType.LPWStr)] string lpstrText, [In, MarshalAs(UnmanagedType.LPWStr)] string lpstrCaption, [In] uint dwType, [In, MarshalAs(UnmanagedType.LPWStr)] string lpstrHelpFile, [In] uint dwHelpContext, [ComAliasName("ExtendedWebBrowser2.UnsafeNativeMethods.LONG_PTR")] out int plResult);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ShowHelp([In, ComAliasName("ExtendedWebBrowser2.UnsafeNativeMethods.wireHWND")] ref _RemotableHandle hwnd, [In, MarshalAs(UnmanagedType.LPWStr)] string pszHelpFile, [In] uint uCommand, [In] uint dwData, [In] tagPOINT ptMouse, [Out, MarshalAs(UnmanagedType.IDispatch)] object pDispatchObjectHit);
        }
        #endregion
    }
}