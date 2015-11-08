/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace Little_System_Cleaner.Misc
{
    public class RegEditGo : IDisposable
    {
        public RegEditGo()
        {
            uint processId;

            // Checks if access is disabled to regedit, and adds access to it
            CheckAccess();

            var processes = Process.GetProcessesByName("RegEdit");
            if (processes.Length == 0)
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "RegEdit.exe";
                    process.Start();

                    process.WaitForInputIdle();

                    _wndApp = process.MainWindowHandle;
                    processId = (uint) process.Id;
                }
            }
            else
            {
                _wndApp = processes[0].MainWindowHandle;
                processId = (uint) processes[0].Id;

                Interop.SetForegroundWindow(_wndApp);
            }

            if (_wndApp == IntPtr.Zero)
            {
                ShowErrorMessage(new SystemException("no app handle"));
            }

            // get handle to treeview
            _wndTreeView = Interop.FindWindowEx(_wndApp, IntPtr.Zero, "SysTreeView32", null);
            if (_wndTreeView == IntPtr.Zero)
            {
                ShowErrorMessage(new SystemException("no treeview"));
            }

            // get handle to listview
            _wndListView = Interop.FindWindowEx(_wndApp, IntPtr.Zero, "SysListView32", null);
            if (_wndListView == IntPtr.Zero)
            {
                ShowErrorMessage(new SystemException("no listview"));
            }


            // allocate buffer in local process
            _lpLocalBuffer = Marshal.AllocHGlobal(dwBufferSize);
            if (_lpLocalBuffer == IntPtr.Zero)
                ShowErrorMessage(new SystemException("Failed to allocate memory in local process"));

            _hProcess = Interop.OpenProcess(Interop.PROCESS_ALL_ACCESS, false, processId);
            if (_hProcess == IntPtr.Zero)
                ShowErrorMessage(new ApplicationException("Failed to access process"));

            // Allocate a buffer in the remote process
            _lpRemoteBuffer = Interop.VirtualAllocEx(_hProcess, IntPtr.Zero, dwBufferSize, Interop.MEM_COMMIT,
                Interop.PAGE_READWRITE);
            if (_lpRemoteBuffer == IntPtr.Zero)
                ShowErrorMessage(new SystemException("Failed to allocate memory in remote process"));
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        ~RegEditGo()
        {
            Dispose(false);
        }

        public void Close()
        {
            Dispose();
        }

        #region public

        /// <summary>
        ///     Opens RegEdit.exe and navigates to given registry path and value
        /// </summary>
        /// <param name="keyPath">path of registry key</param>
        /// <param name="valueName">name of registry value (can be null)</param>
        internal static void GoTo(string keyPath, string valueName)
        {
            using (var locator = new RegEditGo())
            {
                var hasValue = !string.IsNullOrEmpty(valueName);
                locator.OpenKey(keyPath, hasValue);

                if (hasValue)
                {
                    Thread.Sleep(200);
                    locator.OpenValue(valueName);
                }
            }
        }

        public void OpenKey(string path, bool select)
        {
            if (string.IsNullOrEmpty(path)) return;

            const int TVGN_CARET = 0x0009;

            if (path.StartsWith("HKLM"))
            {
                path = "HKEY_LOCAL_MACHINE" + path.Remove(0, 4);
            }
            else if (path.StartsWith("HKCU"))
            {
                path = "HKEY_CURRENT_USER" + path.Remove(0, 4);
            }
            else if (path.StartsWith("HKCR"))
            {
                path = "HKEY_CLASSES_ROOT" + path.Remove(0, 4);
            }

            Interop.SendMessage(_wndTreeView, Interop.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);

            var tvItem = Interop.SendMessage(_wndTreeView, Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_ROOT,
                IntPtr.Zero);
            foreach (var key in path.Split('\\'))
            {
                if (key.Length == 0) continue;

                tvItem = FindKey(tvItem, key);
                if (tvItem == IntPtr.Zero)
                {
                    return;
                }
                Interop.SendMessage(_wndTreeView, Interop.TVM_SELECTITEM, (IntPtr) TVGN_CARET, tvItem);

                // expand tree node 
                const int VK_RIGHT = 0x27;
                Interop.SendMessage(_wndTreeView, Interop.WM_KEYDOWN, (IntPtr) VK_RIGHT, IntPtr.Zero);
                Interop.SendMessage(_wndTreeView, Interop.WM_KEYUP, (IntPtr) VK_RIGHT, IntPtr.Zero);
            }

            Interop.SendMessage(_wndTreeView, Interop.TVM_SELECTITEM, (IntPtr) TVGN_CARET, tvItem);

            if (select)
            {
                Interop.BringWindowToTop(_wndApp);
            }
            else
            {
                SendTabKey(false);
            }
        }

        public void OpenValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            Interop.SendMessage(_wndListView, Interop.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);

            if (value.Length == 0)
            {
                SetLVItemState(0);
                return;
            }

            var item = 0;
            for (;;)
            {
                var itemText = GetLVItemText(item);
                if (itemText == null)
                {
                    return;
                }
                if (string.Compare(itemText, value, true) == 0)
                {
                    break;
                }
                item++;
            }

            SetLVItemState(item);


            const int LVM_FIRST = 0x1000;
            const int LVM_ENSUREVISIBLE = LVM_FIRST + 19;
            Interop.SendMessage(_wndListView, LVM_ENSUREVISIBLE, (IntPtr) item, IntPtr.Zero);

            Interop.BringWindowToTop(_wndApp);

            SendTabKey(false);
            SendTabKey(true);
        }

        #endregion

        #region private

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            if (_lpLocalBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(_lpLocalBuffer);
            if (_lpRemoteBuffer != IntPtr.Zero)
                Interop.VirtualFreeEx(_hProcess, _lpRemoteBuffer, 0, Interop.MEM_RELEASE);
            if (_hProcess != IntPtr.Zero)
                Interop.CloseHandle(_hProcess);
        }

        private const int dwBufferSize = 1024;

        private readonly IntPtr _wndApp;
        private readonly IntPtr _wndTreeView;
        private readonly IntPtr _wndListView;

        private readonly IntPtr _hProcess;
        private IntPtr _lpRemoteBuffer;
        private IntPtr _lpLocalBuffer;

        private void SendTabKey(bool shiftPressed)
        {
            const int VK_TAB = 0x09;
            const int VK_SHIFT = 0x10;
            if (!shiftPressed)
            {
                Interop.PostMessage(_wndApp, Interop.WM_KEYDOWN, VK_TAB, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYUP, VK_TAB, 0x1f01);
            }
            else
            {
                Interop.PostMessage(_wndApp, Interop.WM_KEYDOWN, VK_SHIFT, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYDOWN, VK_TAB, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYUP, VK_TAB, 0x1f01);
                Interop.PostMessage(_wndApp, Interop.WM_KEYUP, VK_SHIFT, 0x1f01);
            }
        }


        private string GetTVItemTextEx(IntPtr wndTreeView, IntPtr item)
        {
            const int TVIF_TEXT = 0x0001;
            const int MAX_TVITEMTEXT = 512;

            // set address to remote buffer immediately following the tvItem
            var nRemoteBufferPtr = _lpRemoteBuffer.ToInt64() + Marshal.SizeOf(typeof (Interop.TVITEM));

            var tvi = new Interop.TVITEM
            {
                mask = TVIF_TEXT,
                hItem = item,
                cchTextMax = MAX_TVITEMTEXT,
                pszText = (IntPtr) nRemoteBufferPtr
            };

            // copy local tvItem to remote buffer
            var bSuccess = Interop.WriteProcessMemory(_hProcess, _lpRemoteBuffer, ref tvi,
                Marshal.SizeOf(typeof (Interop.TVITEM)), IntPtr.Zero);
            if (!bSuccess)
                ShowErrorMessage(new SystemException("Failed to write to process memory"));

            Interop.SendMessage(wndTreeView, Interop.TVM_GETITEMW, IntPtr.Zero, _lpRemoteBuffer);

            // copy tvItem back into local buffer (copy whole buffer because we don't yet know how big the string is)
            bSuccess = Interop.ReadProcessMemory(_hProcess, _lpRemoteBuffer, _lpLocalBuffer, dwBufferSize, IntPtr.Zero);
            if (!bSuccess)
                ShowErrorMessage(new SystemException("Failed to read from process memory"));

            var nLocalBufferPtr = _lpLocalBuffer.ToInt64() + Marshal.SizeOf(typeof (Interop.TVITEM));

            return Marshal.PtrToStringUni((IntPtr) nLocalBufferPtr);
        }

        private IntPtr FindKey(IntPtr itemParent, string key)
        {
            var itemChild = Interop.SendMessage(_wndTreeView, Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_CHILD,
                itemParent);
            while (itemChild != IntPtr.Zero)
            {
                if (string.Compare(GetTVItemTextEx(_wndTreeView, itemChild), key, true) == 0)
                {
                    return itemChild;
                }
                itemChild = Interop.SendMessage(_wndTreeView, Interop.TVM_GETNEXTITEM, (IntPtr) Interop.TVGN_NEXT,
                    itemChild);
            }
            ShowErrorMessage(new SystemException($"TVM_GETNEXTITEM failed... key '{key}' not found!"));
            return IntPtr.Zero;
        }

        private void SetLVItemState(int item)
        {
            const int LVM_FIRST = 0x1000;
            const int LVM_SETITEMSTATE = LVM_FIRST + 43;
            const int LVIF_STATE = 0x0008;

            const int LVIS_FOCUSED = 0x0001;
            const int LVIS_SELECTED = 0x0002;

            var lvItem = new Interop.LVITEM
            {
                mask = LVIF_STATE,
                iItem = item,
                iSubItem = 0,
                state = LVIS_FOCUSED | LVIS_SELECTED,
                stateMask = LVIS_FOCUSED | LVIS_SELECTED
            };

            // copy local lvItem to remote buffer
            var bSuccess = Interop.WriteProcessMemory(_hProcess, _lpRemoteBuffer, ref lvItem,
                Marshal.SizeOf(typeof (Interop.LVITEM)), IntPtr.Zero);
            if (!bSuccess)
                ShowErrorMessage(new SystemException("Failed to write to process memory"));

            // Send the message to the remote window with the address of the remote buffer
            if (Interop.SendMessage(_wndListView, LVM_SETITEMSTATE, (IntPtr) item, _lpRemoteBuffer) == IntPtr.Zero)
                ShowErrorMessage(new SystemException("LVM_GETITEM Failed "));
        }

        private string GetLVItemText(int item)
        {
            const int LVM_GETITEM = 0x1005;
            const int LVIF_TEXT = 0x0001;

            // set address to remote buffer immediately following the lvItem 
            var nRemoteBufferPtr = _lpRemoteBuffer.ToInt64() + Marshal.SizeOf(typeof (Interop.TVITEM));

            var lvItem = new Interop.LVITEM
            {
                mask = LVIF_TEXT,
                iItem = item,
                iSubItem = 0,
                pszText = (IntPtr) nRemoteBufferPtr,
                cchTextMax = 50
            };

            // copy local lvItem to remote buffer
            var bSuccess = Interop.WriteProcessMemory(_hProcess, _lpRemoteBuffer, ref lvItem,
                Marshal.SizeOf(typeof (Interop.LVITEM)), IntPtr.Zero);
            if (!bSuccess)
                ShowErrorMessage(new SystemException("Failed to write to process memory"));

            // Send the message to the remote window with the address of the remote buffer
            if (Interop.SendMessage(_wndListView, LVM_GETITEM, IntPtr.Zero, _lpRemoteBuffer) == IntPtr.Zero)
                return null;

            // copy lvItem back into local buffer (copy whole buffer because we don't yet know how big the string is)
            bSuccess = Interop.ReadProcessMemory(_hProcess, _lpRemoteBuffer, _lpLocalBuffer, dwBufferSize, IntPtr.Zero);
            if (!bSuccess)
                ShowErrorMessage(new SystemException("Failed to read from process memory"));

            var nLocalBufferPtr = _lpLocalBuffer.ToInt64() + Marshal.SizeOf(typeof (Interop.TVITEM));
            return Marshal.PtrToStringAnsi((IntPtr) nLocalBufferPtr);
        }

        private void CheckAccess()
        {
            using (
                var regKey =
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
            {
                var n = regKey?.GetValue("DisableRegistryTools") as int?;

                // Value doesnt exists
                if (n == null)
                    return;

                // User has access
                if (n.Value == 0)
                    return;

                // Value is either 1 or 2 which means we cant access regedit.exe

                // So, lets enable access
                regKey.SetValue("DisableRegistryTools", 0, RegistryValueKind.DWord);
            }
        }

        private void ShowErrorMessage(Exception ex)
        {
#if (DEBUG)
            throw ex;
#endif
        }

        private static class Interop
        {
            internal const uint PROCESS_ALL_ACCESS = (uint) (0x000F0000L | 0x00100000L | 0xFFF);
            internal const uint MEM_COMMIT = 0x1000;
            internal const uint MEM_RELEASE = 0x8000;
            internal const uint PAGE_READWRITE = 0x04;

            internal const int WM_SETFOCUS = 0x0007;
            internal const int WM_KEYDOWN = 0x0100;
            internal const int WM_KEYUP = 0x0101;
            internal const int TVM_GETNEXTITEM = 0x1100 + 10;
            internal const int TVM_SELECTITEM = 0x1100 + 11;
            internal const int TVM_GETITEMW = 0x1100 + 62;
            internal const int TVGN_ROOT = 0x0000;
            internal const int TVGN_NEXT = 0x0001;
            internal const int TVGN_CHILD = 0x0004;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
                string lpszWindow);

            [DllImport("kernel32")]
            internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

            [DllImport("kernel32")]
            internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize,
                uint flAllocationType, uint flProtect);

            [DllImport("kernel32")]
            internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);

            [DllImport("kernel32")]
            internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref LVITEM buffer,
                int dwSize, IntPtr lpNumberOfBytesWritten);

            [DllImport("kernel32")]
            internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref TVITEM buffer,
                int dwSize, IntPtr lpNumberOfBytesWritten);

            [DllImport("kernel32")]
            internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer,
                int dwSize, IntPtr lpNumberOfBytesRead);

            [DllImport("kernel32")]
            internal static extern bool CloseHandle(IntPtr hObject);


            [DllImport("user32.dll")]
            internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            internal static extern int PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

            [DllImport("user32.dll")]
            internal static extern bool BringWindowToTop(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetForegroundWindow(IntPtr hWnd);

            #region structs

            /// <summary>
            ///     from 'http://dotnetjunkies.com/WebLog/chris.taylor/'
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct LVITEM
            {
                public uint mask;
                public int iItem;
                public int iSubItem;
                public uint state;
                public uint stateMask;
                public IntPtr pszText;
                public int cchTextMax;
                public readonly int iImage;
            }

            /// <summary>
            ///     from '.\PlatformSDK\Include\commctrl.h'
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct TVITEM
            {
                public uint mask;
                public IntPtr hItem;
                public readonly uint state;
                public readonly uint stateMask;
                public IntPtr pszText;
                public int cchTextMax;
                public readonly uint iImage;
                public readonly uint iSelectedImage;
                public readonly uint cChildren;
                public readonly IntPtr lParam;
            }

            #endregion
        }

        #endregion
    }
}