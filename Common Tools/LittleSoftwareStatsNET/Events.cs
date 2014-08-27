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
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace LittleSoftwareStats
{
    public class Events : CollectionBase
    {
        public Event this[int index]
        {
            get { return (Event)this.InnerList[index]; }
            set { this.InnerList[index] = value; }
        }

        public Events()
        {
        }

        public int Add(Event eventData)
        {
            return this.InnerList.Add(eventData);
        }

        public bool Contains(Event eventData)
        {
            return this.InnerList.Contains(eventData);
        }

        public int IndexOf(Event eventData)
        {
            return this.InnerList.IndexOf(eventData);
        }

    }

    public class Event : DictionaryBase
    {
        public DictionaryEntry this[int index]
        {
            get { return (DictionaryEntry)this.InnerHashtable[index]; }
            set { this.InnerHashtable[index] = value; }
        }

        public Event(string eventCode, string sessionId, int flowId = 0) 
        {
            this.Add("tp", eventCode);
            this.Add("ss", sessionId);
            this.Add("ts", Utils.GetUnixTime());

            if (flowId != 0)
                this.Add("fl", flowId);
        }

        public void Add(string name, object value) 
        {
            if (string.IsNullOrEmpty(name))
                return;

            this.InnerHashtable.Add(name, value);
        }
    }
}
