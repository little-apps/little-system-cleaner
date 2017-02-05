﻿/*
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
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Shared
{
    public class SysRestore
    {
        // Constants
        internal const short BeginSystemChange = 100; // Start of operation

        internal const short EndSystemChange = 101; // End of operation

        // Windows XP only - used to prevent the restore points intertwined
        internal const short BeginNestedSystemChange = 102;

        internal const short EndNestedSystemChange = 103;

        internal const short DesktopSetting = 2; /* not implemented */
        internal const short AccessibilitySetting = 3; /* not implemented */
        internal const short OeSetting = 4; /* not implemented */
        internal const short ApplicationRun = 5; /* not implemented */
        internal const short WindowsShutdown = 8; /* not implemented */
        internal const short WindowsBoot = 9; /* not implemented */
        internal const short MaxDesc = 64;
        internal const short MaxDescW = 256;

        [DllImport("srclient.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SRSetRestorePointW(ref RestorePointInfo pRestorePtSpec,
            out STATEMGRSTATUS pSMgrStatus);

        /// <summary>
        ///     Verifies that the OS can do system restores
        /// </summary>
        /// <returns>True if OS is able to perform system restores</returns>
        public static bool SysRestoreAvailable()
        {
            var majorVersion = Environment.OSVersion.Version.Major;
            var minorVersion = Environment.OSVersion.Version.Minor;

            // See if it is enabled
            if (!Settings.Default.optionsSysRestore)
                return false;

            // See if DLL exists
            if (Utils.SearchPath("srclient.dll"))
                return true;

            // Windows ME
            if (majorVersion == 4 && minorVersion == 90)
                return true;

            // Windows XP
            if (majorVersion == 5 && minorVersion == 1)
                return true;

            // Windows Vista
            if (majorVersion == 6 && minorVersion == 0)
                return true;

            // Windows 7
            if (majorVersion == 6 && minorVersion == 1)
                return true;

            // Windows 8 + 8.1
            if (majorVersion == 6 && (minorVersion == 2 || minorVersion == 3))
                return true;

            // All others : Win 95, 98, 2000, Server
            return false;
        }

        /// <summary>
        ///     Starts system restore
        ///     Use SysRestore.EndRestore or SysRestore.CancelRestore to end the system restore
        /// </summary>
        /// <param name="description">The description of the restore</param>
        /// <param name="lSeqNum">Returns the sequence number</param>
        /// <exception cref="System.ComponentModel.Win32Exception">
        ///     Thrown when STATEMGRSTATUS.nStatus doesn't equal 0
        ///     (ERROR_SUCCESS)
        /// </exception>
        public static void StartRestore(string description, out long lSeqNum)
        {
            var rpInfo = new RestorePointInfo();
            var rpStatus = new STATEMGRSTATUS();

            if (!SysRestoreAvailable())
            {
                lSeqNum = 0;
                return;
            }

            try
            {
                // Prepare Restore Point
                rpInfo.dwEventType = BeginSystemChange;
                // By default we create a verification system
                rpInfo.dwRestorePtType = (int)RestoreType.Restore;
                rpInfo.llSequenceNumber = 0;
                rpInfo.szDescription = description;

                SRSetRestorePointW(ref rpInfo, out rpStatus);
            }
            catch (DllNotFoundException)
            {
                rpStatus.nStatus = 2;
            }

            if (rpStatus.nStatus != 0)
            {
                lSeqNum = 0;
                throw new Win32Exception(rpStatus.nStatus);
            }

            lSeqNum = rpStatus.llSequenceNumber;
        }

        /// <summary>
        ///     Ends system restore call
        /// </summary>
        /// <param name="lSeqNum">The restore sequence number</param>
        /// <exception cref="System.ComponentModel.Win32Exception">
        ///     Thrown when STATEMGRSTATUS.nStatus doesn't equal 0
        ///     (ERROR_SUCCESS)
        /// </exception>
        public static void EndRestore(long lSeqNum)
        {
            var rpInfo = new RestorePointInfo();
            var rpStatus = new STATEMGRSTATUS();

            if (!SysRestoreAvailable())
                return;

            try
            {
                rpInfo.dwEventType = EndSystemChange;
                rpInfo.llSequenceNumber = lSeqNum;

                SRSetRestorePointW(ref rpInfo, out rpStatus);
            }
            catch (DllNotFoundException)
            {
                rpStatus.nStatus = 2;
            }

            if (rpStatus.nStatus != 0)
                throw new Win32Exception(rpStatus.nStatus);
        }

        /// <summary>
        ///     Cancels restore call
        /// </summary>
        /// <param name="lSeqNum">The restore sequence number</param>
        /// <exception cref="System.ComponentModel.Win32Exception">
        ///     Thrown when STATEMGRSTATUS.nStatus doesn't equal 0
        ///     (ERROR_SUCCESS)
        /// </exception>
        public static void CancelRestore(long lSeqNum)
        {
            var rpInfo = new RestorePointInfo();
            var rpStatus = new STATEMGRSTATUS();

            if (!SysRestoreAvailable())
                return;

            try
            {
                rpInfo.dwEventType = EndSystemChange;
                rpInfo.dwRestorePtType = (int)RestoreType.CancelledOperation;
                rpInfo.llSequenceNumber = lSeqNum;

                SRSetRestorePointW(ref rpInfo, out rpStatus);
            }
            catch (DllNotFoundException)
            {
                rpStatus.nStatus = 2;
            }

            if (rpStatus.nStatus != 0)
                throw new Win32Exception(rpStatus.nStatus);
        }

        /// <summary>
        ///     Contains information used by the SRSetRestorePoint function
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct RestorePointInfo
        {
            public int dwEventType; // The type of event
            public int dwRestorePtType; // The type of restore point
            public long llSequenceNumber; // The sequence number of the restore point

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxDescW + 1)]
            public string szDescription;

            // The description to be displayed so the user can easily identify a restore point
        }

        /// <summary>
        ///     Contains status information used by the SRSetRestorePoint function
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct STATEMGRSTATUS
        {
            public int nStatus; // The status code
            public long llSequenceNumber; // The sequence number of the restore point
        }

        // Type of restorations
        internal enum RestoreType
        {
            ApplicationInstall = 0, // Installing a new application
            ApplicationUninstall = 1, // An application has been uninstalled
            ModifySettings = 12, // An application has had features added or removed
            CancelledOperation = 13, // An application needs to delete the restore point it created
            Restore = 6, // System Restore
            Checkpoint = 7, // Checkpoint
            DeviceDriverInstall = 10, // Device driver has been installed
            FirstRun = 11, // Program used for 1st time
            BackupRecovery = 14 // Restoring a backup
        }
    }
}