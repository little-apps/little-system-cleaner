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
using System.Security.Principal;

namespace Little_System_Cleaner.Misc
{
    public static class Permissions
    {
        /// <summary>
        ///     Checks if the user is an admin
        /// </summary>
        /// <returns>True if it is in admin group</returns>
        internal static bool IsUserAdministrator
        {
            get
            {
                //bool value to hold our return value
                var isAdmin = false;
                try
                {
                    //get the currently logged in user
                    var user = WindowsIdentity.GetCurrent();
                    var principal = new WindowsPrincipal(user);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine(
                        $"An UnauthorizedAccessException was thrown trying to determine if user is admin ({ex.Message})");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"An unknown error occurred trying to determine if user is admin ({ex.Message})");
                }
                return isAdmin;
            }
        }

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
                var hproc = Process.GetCurrentProcess().Handle;
                var htok = IntPtr.Zero;

                if (!PInvoke.OpenProcessToken(hproc, PInvoke.TokenAdjustPrivileges | PInvoke.TokenQuery, ref htok))
                    return false;

                var tp = new PInvoke.TokPriv1Luid
                {
                    Count = 1,
                    Luid = 0,
                    Attr = enabled ? PInvoke.SePrivilegeEnabled : PInvoke.SePrivilegeRemoved
                };

                if (!PInvoke.LookupPrivilegeValue(null, privilege, ref tp.Luid))
                    return false;

                var retVal = PInvoke.AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);

                // Cleanup
                PInvoke.CloseHandle(htok);

                return retVal;
            }
            catch
            {
                return false;
            }
        }
    }
}