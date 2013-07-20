using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace System.Agent.Privacy
{
    internal class Hook
    {
        #region NestedTypes
        /// <summary>
        /// 底层键盘钩子
        /// </summary>
        private const int idHook = 13;
        /// <summary>
        /// 钩子委托声明
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);
        /// <summary>
        /// 安装钩子
        /// </summary>
        /// <param name="idHook"></param>
        /// <param name="lpfn"></param>
        /// <param name="hInstance"></param>
        /// <param name="threadId"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr pInstance, int threadId);
        /// <summary>
        /// 卸载钩子
        /// </summary>
        /// <param name="idHook"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(IntPtr pHookHandle);
        /// <summary>
        /// 传递钩子
        /// </summary>
        /// <param name="pHookHandle">是您自己的钩子函数的句柄。用该句柄可以遍历钩子链</param>
        /// <param name="nCode">把传入的参数简单传给CallNextHookEx即可</param>
        /// <param name="wParam">把传入的参数简单传给CallNextHookEx即可</param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(IntPtr pHookHandle, int nCode, Int32 wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyStructure
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        #endregion

        #region Fields
        public event KeyEventHandler KeyDown;
        /// <summary>
        /// 键盘钩子句柄
        /// </summary>
        private IntPtr m_pKeyboardHook;
        /// <summary>
        /// 键盘钩子委托实例
        /// </summary>
        /// <remarks>
        /// 不要试图省略此变量,否则将会导致
        /// 激活 CallbackOnCollectedDelegate 托管调试助手 (MDA)。 
        /// 详细请参见MSDN中关于 CallbackOnCollectedDelegate 的描述
        /// </remarks>
        private HookProc m_KeyboardHookProcedure;
        #endregion

        #region Methods
        /// <summary>
        /// 安装钩子
        /// </summary>
        /// <returns></returns>
        public bool Install()
        {
            IntPtr pInstance = GetModuleHandle("user32");
            if (pInstance == IntPtr.Zero)
            {
                pInstance = Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().ManifestModule);
                if (pInstance == IntPtr.Zero)
                {
                    pInstance = (IntPtr)4194304;
                }
            }
            if (m_pKeyboardHook == IntPtr.Zero)
            {
                m_KeyboardHookProcedure = new HookProc(this.KeyboardHookProc);
                m_pKeyboardHook = SetWindowsHookEx(idHook, m_KeyboardHookProcedure, pInstance, 0);
                if (m_pKeyboardHook == IntPtr.Zero)
                {
                    Uninstall();
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 键盘钩子处理函数
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        /// <remarks>此版本的键盘事件处理不是很好,还有待修正.</remarks>
        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            var key = (KeyStructure)Marshal.PtrToStructure(lParam, typeof(KeyStructure));
            Keys code = (Keys)key.vkCode;
            if (this.KeyDown != null)
            {
                this.KeyDown(this, new KeyEventArgs(code));
            }
            switch (code)
            {
                case Keys.LWin:
                case Keys.RWin:
                case Keys.Control:
                case Keys.Alt:
                case Keys.Delete:
                case Keys.F4:
                case Keys.Tab:
                case Keys.Escape:
                    return 1;
            }
            return 0;
        }

        /// <summary>
        /// 卸载钩子
        /// </summary>
        /// <returns></returns>
        public void Uninstall()
        {
            if (m_pKeyboardHook == IntPtr.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(m_pKeyboardHook);
            m_pKeyboardHook = IntPtr.Zero;
        }
        #endregion
    }
}