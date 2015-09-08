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
using System.Security.Principal;
using System.Diagnostics;

namespace Little_System_Cleaner.Misc
{
    public class Permissions
    {
        internal static void SetPrivileges(bool enabled)
        {
            SetPrivilege("SeShutdownPrivilege", enabled);
            SetPrivilege("SeBackupPrivilege", enabled);
            SetPrivilege("SeRestorePrivilege", enabled);
            SetPrivilege("SeDebugPrivilege", enabled);
        }

        internal static bool SetPrivilege(string privilege, bool enabled)
        {
            try
            {
                PInvoke.TokPriv1Luid tp = new PInvoke.TokPriv1Luid();
                IntPtr hproc = Process.GetCurrentProcess().Handle;
                IntPtr htok = IntPtr.Zero;

                if (!PInvoke.OpenProcessToken(hproc, PInvoke.TOKEN_ADJUST_PRIVILEGES | PInvoke.TOKEN_QUERY, ref htok))
                    return false;

                tp.Count = 1;
                tp.Luid = 0;
                tp.Attr = ((enabled) ? (PInvoke.SE_PRIVILEGE_ENABLED) : (PInvoke.SE_PRIVILEGE_REMOVED));

                if (!PInvoke.LookupPrivilegeValue(null, privilege, ref tp.Luid))
                    return false;

                bool bRet = (PInvoke.AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero));

                // Cleanup
                PInvoke.CloseHandle(htok);

                return bRet;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the user is an admin
        /// </summary>
        /// <returns>True if it is in admin group</returns>
        internal static bool IsUserAdministrator
        {
            get
            {
                //bool value to hold our return value
                bool isAdmin;
                try
                {
                    //get the currently logged in user
                    WindowsIdentity user = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(user);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch (UnauthorizedAccessException)
                {
                    isAdmin = false;
#if (DEBUG)
                    throw;
#endif
                }
                catch (Exception)
                {
                    isAdmin = false;
#if (DEBUG)
                    throw;
#endif
                }
                return isAdmin;
            }
        }
    }
}
