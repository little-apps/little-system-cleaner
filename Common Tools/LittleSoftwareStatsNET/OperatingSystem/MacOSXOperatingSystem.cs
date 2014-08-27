/*
 * Little Software Stats - .NET Library
 * Copyright (C) 2008-2012 Little Apps (http://www.little-apps.org)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LittleSoftwareStats.OperatingSystem
{
    internal class MacOSXOperatingSystem : UnixOperatingSystem
    {
        public MacOSXOperatingSystem()
        {

        }

        public override int Architecture
        {
            get { return 64; }
        }

        public override string Version
        {
            get
            {
                Regex regex = new Regex(@"System Version:\s(?<version>[\w\s\d\.]*)\s");
                MatchCollection matches = regex.Matches(Utils.SystemProfilerCommandOutput);
                return matches[0].Groups["version"].Value;
            }
        }

        Hardware.Hardware _hardware;
        public override Hardware.Hardware Hardware
        {
            get
            {
                if (_hardware == null)
                    _hardware = new Hardware.MacOSXHardware();
                return _hardware;
            }
        }

    }
}
