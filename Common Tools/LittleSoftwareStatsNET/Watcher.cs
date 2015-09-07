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
using LittleSoftwareStats.MachineIdentifiers;

namespace LittleSoftwareStats
{
    public class Watcher
    {
        private readonly Events _array = new Events();
        private readonly Cache _cache = new Cache();

        private IMachineIdentifierProvider _identifierService;
        private string _uniqueId;
        private string UniqueId
        {
            get
            {
                if (string.IsNullOrEmpty(_uniqueId))
                {
                    _identifierService = new MachineIdentifierProvider(new IMachineIdentifier[] { new MachineNameIdentifier(), new NetworkAdapterIdentifier(), new VolumeInfoIdentifier() });

                    _uniqueId = _identifierService.MachineHash;
                }

                return _uniqueId;
            }
        }

        private string _sessionId;
        private string SessionId
        {
            get
            {
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    return _sessionId;
                }
                _sessionId = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
                return _sessionId;
            }
        }

        protected int _flowNumber;
        protected int FlowNumber
        {
            get
            {
                _flowNumber = _flowNumber + 1;
                return _flowNumber;
            }
        }

        public bool Started { get; private set; }

        /// <summary>
        /// Starts tracking software
        /// </summary>
        /// <remarks>Config.Enabled must be set to true</remarks>
        /// <param name="appId">Application ID</param>
        /// <param name="appVer">Application Version</param>
        public void Start(string appId, string appVer) {
            if (Started || !Config.Enabled)
                return;

            Event e = new Event("strApp", SessionId);

            Config.AppId = appId;
            Config.AppVer = appVer;

            // Get os + hardware config
            OperatingSystem.OperatingSystem osInfo = OperatingSystem.OperatingSystem.GetOperatingSystemInfo();
            Hardware.Hardware hwInfo = osInfo.Hardware;

            e.Add("ID", UniqueId);
            e.Add("aid", appId);
            e.Add("aver", appVer);

            e.Add("osv", osInfo.Version);
            e.Add("ossp", osInfo.ServicePack);
            e.Add("osar", osInfo.Architecture);
            e.Add("osjv", osInfo.JavaVersion);
            e.Add("osnet", osInfo.FrameworkVersion);
            e.Add("osnsp", osInfo.FrameworkSP);
            e.Add("oslng", osInfo.Lcid);
            e.Add("osscn", hwInfo.ScreenResolution);

            e.Add("cnm", hwInfo.CpuName);
            e.Add("car", hwInfo.CpuArchitecture);
            e.Add("cbr", hwInfo.CpuBrand);
            e.Add("cfr", hwInfo.CpuFrequency);
            e.Add("ccr", hwInfo.CpuCores);
            e.Add("mtt", hwInfo.MemoryTotal);
            e.Add("mfr", hwInfo.MemoryFree);
            e.Add("dtt", hwInfo.DiskTotal);
            e.Add("dfr", hwInfo.DiskFree);

            _array.Add(e);

            Started = true;
        }

        /// <summary>
        /// Stops tracking software and sends data to API
        /// </summary>
        public void Stop()
        {
            if (!Started)
                return;

            _array.Add(new Event("stApp", SessionId));

            try
            {
                string data = _cache.GetPostData(_array);

                Utils.SendPostData(data);
                _cache.Delete();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                _cache.SaveCacheToFile(_array);
            }

            Started = false;
        }

        /// <summary>
        /// Track an event
        /// </summary>
        /// <param name="categoryName">Event category</param>
        /// <param name="eventName">Event name</param>
        public void Event(string categoryName, string eventName)
        {
            if (!Started)
                return;

            Event e = new Event("ev", SessionId, FlowNumber) {{"ca", categoryName}, {"nm", eventName}};


            _array.Add(e);
        }

        /// <summary>
        /// Track an event value
        /// </summary>
        /// <param name="categoryName">Event category</param>
        /// <param name="eventName">Event Name</param>
        /// <param name="eventValue">Event Value</param>
        public void EventValue(string categoryName, string eventName, string eventValue)
        {
            if (!Started)
                return;

            Event e = new Event("evV", SessionId, FlowNumber)
            {
                {"ca", categoryName},
                {"nm", eventName},
                {"vl", eventValue}
            };


            _array.Add(e);
        }

        /// <summary>
        /// Track an event period
        /// </summary>
        /// <param name="categoryName">Event category</param>
        /// <param name="eventName">Event name</param>
        /// <param name="eventDuration">Event duration (in seconds)</param>
        /// <param name="eventCompleted">Did the event complete?</param>
        public void EventPeriod(string categoryName, string eventName, int eventDuration, bool eventCompleted)
        {
            if (!Started)
                return;

            Event e = new Event("evP", SessionId, FlowNumber)
            {
                {"ca", categoryName},
                {"nm", eventName},
                {"tm", eventDuration},
                {"ec", (eventCompleted) ? (1) : (0)}
            };
            
            _array.Add(e);
        }

        /// <summary>
        /// Track a log message
        /// </summary>
        /// <param name="logMessage">Message to log</param>
        public void Log(string logMessage)
        {
            if (!Started)
                return;

            Event e = new Event("lg", SessionId, FlowNumber) {{"ms", logMessage}};
            
            _array.Add(e);
        }

        public enum Licenses { Free, Trial, Registered, Demo, Cracked };

        /// <summary>
        /// Track software license
        /// </summary>
        /// <param name="l">License type (Free, Trial, Registered, Demo, Cracked)</param>
        public void License(Licenses l)
        {
            if (!Started)
                return;

            Event e = new Event("ctD", SessionId, FlowNumber) {{"nm", "License"}};
            
            string licenseType = "";
            switch (l)
            {
                case Licenses.Free:
                    {
                        licenseType = "F";
                        break;
                    }
                case Licenses.Trial:
                    {
                        licenseType = "T";
                        break;
                    }
                case Licenses.Demo:
                    {
                        licenseType = "D";
                        break;
                    }
                case Licenses.Registered:
                    {
                        licenseType = "R";
                        break;
                    }
                case Licenses.Cracked:
                    {
                        licenseType = "C";
                        break;
                    }
            }

            e.Add("vl", licenseType);

            _array.Add(e);
        }

        /// <summary>
        /// Track custom data
        /// </summary>
        /// <param name="dataName">Name</param>
        /// <param name="dataValue">Value</param>
        public void CustomData(string dataName, string dataValue)
        {
            if (!Started)
                return;

            Event e = new Event("ctD", SessionId, FlowNumber) {{"nm", dataName}, {"vl", dataValue}};
            
            _array.Add(e);
        }

        /// <summary>
        /// Track an exception
        /// </summary>
        /// <param name="ex">Exception</param>
        public void Exception(Exception ex)
        {
            if (!Started)
                return;

            Event e = new Event("exC", SessionId, FlowNumber)
            {
                {"msg", ex.Message},
                {"stk", ex.StackTrace},
                {"src", ex.Source},
                {"tgs", ex.TargetSite}
            };
            
            _array.Add(e);
        }

        /// <summary>
        /// Track an exception
        /// </summary>
        /// <param name="exceptionMsg">Message</param>
        /// <param name="stackTrace">Stack Trace</param>
        /// <param name="exceptionSrc">Source</param>
        /// <param name="targetSite">Target Site</param>
        public void Exception(string exceptionMsg, string stackTrace, string exceptionSrc, string targetSite)
        {
            if (!Started)
                return;

            Event e = new Event("exC", SessionId, FlowNumber)
            {
                {"msg", exceptionMsg},
                {"stk", stackTrace},
                {"src", exceptionSrc},
                {"tgs", targetSite}
            };
            
            _array.Add(e);
        }

        /// <summary>
        /// Track an installation
        /// </summary>
        public void Install()
        {
            if (!Started)
                return;

            Event e = new Event("ist", SessionId, FlowNumber)
            {
                {"ID", UniqueId},
                {"aid", Config.AppId},
                {"aver", Config.AppVer}
            };
            
            _array.Add(e);
        }

        /// <summary>
        /// Track an uninstallation
        /// </summary>
        public void Uninstall()
        {
            if (!Started)
                return;

            Event e = new Event("ust", SessionId, FlowNumber) {{"aid", Config.AppId}, {"aver", Config.AppVer}};
            
            _array.Add(e);
        }
    }
}
