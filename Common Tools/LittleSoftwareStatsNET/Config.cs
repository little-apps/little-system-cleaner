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

namespace LittleSoftwareStats
{
    public static class Config
    {
        public static bool Enabled { get; set; }

        internal static string ApiUrl
        {
            get
            {
                UriBuilder uri = new UriBuilder((Config.ApiSecure) ? ("https") : ("http"), Config.ApiHost, Config.ApiPort, Config.ApiPath, Config.ApiQuery);
                return uri.ToString();
            }
        }
		
        internal static string AppId { get; set; }
        internal static string AppVer { get; set; }

        internal const string ApiHost = "stats.little-apps.org";
        internal const int ApiPort = 80;
        internal const bool ApiSecure = false;
        internal const string ApiPath = "api."+ApiFormat; // If rewrite is disabled, this would be "api.php"
        internal const string ApiQuery = ""; // If rewrite is disabled, this would be "?type="+ApiFormat

        internal const string ApiFormat = "json"; // Can be xml or json
        internal const string ApiUserAgent = "LittleSoftwareStatsNET";
        internal const int ApiTimeout = 25000;
    }
}
