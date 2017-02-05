﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Registry_Cleaner.Helpers.Backup
{
    public class RegistryValue : IEquatable<RegistryValue>, IXmlSerializable
    {
        private string _name;

        public RegistryValue()
        {
        }

        /// <summary>
        ///     Only used for comparing values
        /// </summary>
        /// <param name="valueName">Value name</param>
        public RegistryValue(string valueName)
        {
            _name = valueName;
        }

        public RegistryValue(string valueName, RegistryValueKind type, object value)
        {
            _name = valueName;
            Type = type;
            Value = value;
        }

        /// <summary>
        ///     Returns value name
        /// </summary>
        /// <remarks>Returns string.Empty if default value name</remarks>
        [XmlAttribute("Name")]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    return string.Empty;

                if (_name.ToUpper() == "(DEFAULT)")
                    return string.Empty;

                return _name;
            }
            set { _name = value; }
        }

        [XmlAttribute("Type")]
        public RegistryValueKind Type { get; set; }

        [XmlAttribute("Value")]
        public string XmlValue { get; set; }

        [XmlIgnore]
        public object Value { get; set; }

        #region IXmlSerializable Members

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();

            Name = reader.GetAttribute("Name");

            var type = reader.GetAttribute("Type");
            if (type != null)
                Type = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), type);

            var valByte = new byte[1];
            var buffer = new byte[50];

            if (Type != RegistryValueKind.MultiString)
            {
                using (var ms = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(ms))
                    {
                        int readBytes;
                        while ((readBytes = reader.ReadElementContentAsBase64(buffer, 0, 50)) > 0)
                        {
                            bw.Write(buffer, 0, readBytes);
                        }
                    }

                    valByte = ms.ToArray();
                }
            }

            switch (Type)
            {
                case RegistryValueKind.Binary:
                    {
                        Value = valByte;

                        break;
                    }
                case RegistryValueKind.DWord:
                    {
                        var valStr = Encoding.UTF8.GetString(valByte);

                        var val = Convert.ToUInt32(valStr);

                        Value = val;

                        break;
                    }
                case RegistryValueKind.QWord:
                    {
                        var valStr = Encoding.UTF8.GetString(valByte);

                        var val = Convert.ToUInt64(valStr);

                        Value = val;

                        break;
                    }
                case RegistryValueKind.MultiString:
                    {
                        var strings = new List<string>();

                        if (reader.IsEmptyElement)
                        {
                            Value = strings.ToArray();
                            break;
                        }

                        var children = reader.ReadSubtree();

                        while (children.Read())
                        {
                            if (children.Name != "string" || children.IsEmptyElement)
                                continue;

                            var s = children.ReadString();

                            if (string.IsNullOrWhiteSpace(s))
                            {
                                strings.Add(string.Empty);
                            }
                            else
                            {
                                var base64Bytes = Convert.FromBase64String(s.Trim());
                                var base64Str = Encoding.UTF8.GetString(base64Bytes);

                                strings.Add(base64Str);
                            }
                        }

                        Value = strings.ToArray();

                        break;
                    }
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    {
                        Value = Encoding.UTF8.GetString(valByte);

                        break;
                    }
                default:
                    {
                        Value = valByte;

                        break;
                    }
            }

            if (Value == null)
                Value = string.Empty;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Name", Name);

            writer.WriteAttributeString("Type", Type.ToString());

            string valStr;

            switch (Type)
            {
                case RegistryValueKind.Binary:
                    {
                        var rawBuffer = (byte[])Value;
                        var bufLen = rawBuffer.Length;

                        writer.WriteBase64(rawBuffer, 0, bufLen);

                        break;
                    }
                case RegistryValueKind.DWord: // == REG_DWORD_LITTLE_ENDIAN
                    {
                        var val = Convert.ToUInt32(Value);

                        valStr = Convert.ToString(val);

                        WriteBase64(writer, valStr);

                        break;
                    }
                case RegistryValueKind.QWord: // QWORD, QWORD_LITTLE_ENDIAN (64-bit integer)
                    {
                        var val = Convert.ToUInt64(Value);

                        valStr = Convert.ToString(val);

                        WriteBase64(writer, valStr);

                        break;
                    }
                case RegistryValueKind.MultiString:
                    {
                        var val = (string[])Value;

                        foreach (var s in val)
                        {
                            writer.WriteStartElement("string");

                            WriteBase64(writer, s);

                            writer.WriteEndElement();
                        }

                        break;
                    }

                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    {
                        valStr = (string)Value;

                        WriteBase64(writer, valStr);

                        break;
                    }
                default:
                    {
                        byte[] rawBuffer;
                        int nLen;

                        var bf = new BinaryFormatter();
                        using (var ms = new MemoryStream())
                        {
                            bf.Serialize(ms, Value);
                            nLen = (int)ms.Length;
                            rawBuffer = ms.ToArray();
                        }

                        // Convert the new byte[] into a char[] and then into a string.
                        var asciiChars = new char[Encoding.ASCII.GetCharCount(rawBuffer, 0, nLen)];
                        Encoding.ASCII.GetChars(rawBuffer, 0, nLen, asciiChars, 0);

                        valStr = new string(asciiChars);

                        WriteBase64(writer, valStr);

                        break;
                    }
            }
        }

        private void WriteBase64(XmlWriter writer, string val)
        {
            var byteLen = Encoding.UTF8.GetByteCount(val);
            var bytes = Encoding.UTF8.GetBytes(val);
            writer.WriteBase64(bytes, 0, byteLen);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        #endregion IXmlSerializable Members

        #region IEquatable Members

        public bool Equals(RegistryValue regValue)
        {
            return Name == regValue.Name;
        }

        public override bool Equals(object obj)
        {
            var a = obj as RegistryValue;
            return a != null && Equals(a);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static bool operator ==(RegistryValue regValue1, RegistryValue regValue2)
        {
            if ((object)regValue1 == null || (object)regValue2 == null)
                return Equals(regValue1, regValue2);

            return regValue1.Equals(regValue2);
        }

        public static bool operator !=(RegistryValue regValue1, RegistryValue regValue2)
        {
            if (regValue1 == null || regValue2 == null)
                return !Equals(regValue1, regValue2);

            return !regValue1.Equals(regValue2);
        }

        #endregion IEquatable Members
    }
}