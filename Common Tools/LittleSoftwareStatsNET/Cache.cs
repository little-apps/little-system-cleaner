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
using System.IO;

namespace LittleSoftwareStats
{
    public class Cache
    {
        private string FileName => $@"{Path.GetTempPath()}\{Config.AppId}.{Config.ApiFormat}";

        internal string GetCacheData()
        {
            string fileContents = "";

            if (File.Exists(FileName)) {
                fileContents = Utils.DecodeFrom64(File.ReadAllText(FileName));
            }

            return fileContents;
        }

        internal string GetPostData(Events events)
        {
            string cachedData = GetCacheData();
            string output = "";

            var data = Config.ApiFormat == "json" ? Utils.SerializeAsJson(events) : Utils.SerializeAsXml(events);

            if (string.IsNullOrEmpty(cachedData))
            {
                if (Config.ApiFormat == "json")
                    output = "[" + data + "]";
                else
                    output = "<?xml version=\"1.0\"?><data>" + data + "</data>";
            }
            else
            {
                if (Config.ApiFormat == "json")
                    output += "[" + data;
                else
                    output += "<?xml version=\"1.0\"?><data>" + data;

                foreach (string line in cachedData.Split(Environment.NewLine.ToCharArray()))
                {
                    if (!string.IsNullOrEmpty(line.Trim()))
                    {
                        if (Config.ApiFormat == "json")
                            output += "," + line;
                        else
                            output += line;
                    }
                }

                if (Config.ApiFormat == "json")
                    output += "]";
                else
                    output += "</data>";
            }

            return output.Replace("&", "%26");
        }

        internal void SaveCacheToFile(Events events)
        {
            var data = Config.ApiFormat == "json" ? Utils.SerializeAsJson(events) : Utils.SerializeAsXml(events);

            data += "\n" + GetCacheData();

            Delete();

            File.WriteAllText(FileName, Utils.EncodeTo64(data));
            File.SetAttributes(FileName, FileAttributes.Hidden);
        }

        internal void Delete()
        {
            if (File.Exists(FileName))
                File.Delete(FileName);
        }
    }
}
