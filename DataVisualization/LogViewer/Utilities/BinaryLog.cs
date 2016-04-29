using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using uint8_t = System.Byte;
using System.Diagnostics;

namespace MissionPlanner.Log
{
    /// <summary>
    /// Convert a binary log to an assci log
    /// </summary>
    public class BinaryLog
    {
        private IntPtr _logFormatPtr;
        private byte[] _logFormatBuffer;
        public const byte HEAD_BYTE1 = 0xA3; // Decimal 163  
        public const byte HEAD_BYTE2 = 0x95; // Decimal 149  

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct log_Format
        {
            public uint8_t type;
            public uint8_t length;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] format;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] labels;
        }

        ~BinaryLog()
        {
            if (_logFormatPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_logFormatPtr);
                _logFormatPtr = IntPtr.Zero;
            }
        }

        public struct log_format_cache
        {
            public uint8_t type;
            public uint8_t length;
            public string name;
            public string format;
        }
        
        Dictionary<string, log_Format> logformat = new Dictionary<string, log_Format>();

        void ConvertBinaryToText(string inputfn, string outputfn, bool showui = true)
        {
            using (var stream = File.Open(outputfn, FileMode.Create))
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(inputfn)))
                {
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        byte[] data = ASCIIEncoding.ASCII.GetBytes(ReadMessage(br.BaseStream));
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
        }

        public string ReadMessage(Stream br)
        {
            int log_step = 0;

            long length = br.Length;

            while (br.Position < length)
            {
                byte data = (byte) br.ReadByte();

                switch (log_step)
                {
                    case 0:
                        if (data == HEAD_BYTE1)
                        {
                            log_step++;
                        }
                        break;

                    case 1:
                        if (data == HEAD_BYTE2)
                        {
                            log_step++;
                        }
                        else
                        {
                            log_step = 0;
                        }
                        break;

                    case 2:
                        log_step = 0;
                        try
                        {
                            string line = logEntry(data, br);
                            return line;
                        }
                        catch
                        {
                            Debug.WriteLine("Bad Binary log line {0}", data);
                        }
                        break;
                }
            }

            return "";
        }


        public Tuple<byte, long> ReadMessageTypeOffset(Stream br)
        {
            int log_step = 0;
            long length = br.Length;

            while (br.Position < length)
            {
                byte data = (byte) br.ReadByte();

                switch (log_step)
                {
                    case 0:
                        if (data == HEAD_BYTE1)
                        {
                            log_step++;
                        }
                        break;

                    case 1:
                        if (data == HEAD_BYTE2)
                        {
                            log_step++;
                        }
                        else
                        {
                            log_step = 0;
                        }
                        break;

                    case 2:
                        log_step = 0;
                        try
                        {
                            long pos = br.Position - 3;
                            logEntryFMT(data, br);

                            return new Tuple<byte, long>(data, pos);
                        }
                        catch
                        {
                            Debug.WriteLine("Bad Binary log line {0}", data);
                        }
                        break;
                }
            }

            return null;
        }

        static char[] NullTerminator = new char[] { '\0' };

        log_Format ReadLogFormat(Stream br)
        {
            int len = Marshal.SizeOf<log_Format>();
            if (_logFormatPtr == IntPtr.Zero)
            {
                _logFormatPtr = Marshal.AllocCoTaskMem(len);
                _logFormatBuffer = new byte[len];
            }

            br.Read(_logFormatBuffer, 0, _logFormatBuffer.Length);

            // copy byte array to ptr
            Marshal.Copy(_logFormatBuffer, 0, _logFormatPtr, len);

            log_Format logfmt = Marshal.PtrToStructure<log_Format>(_logFormatPtr);

            string lgname = ASCIIEncoding.ASCII.GetString(logfmt.name).Trim(NullTerminator);

            logformat[lgname] = logfmt;
            return logfmt;
        }

        void logEntryFMT(byte packettype, Stream br)
        {
            switch (packettype)
            {
                case 0x80: // FMT

                    ReadLogFormat(br);
                    return;

                default:
                    string format = "";
                    string name = "";
                    int size = 0;

                    if (packettypecache.ContainsKey(packettype))
                    {
                        var fmt = packettypecache[packettype];
                        name = fmt.name;
                        format = fmt.format;
                        size = fmt.length;
                    }
                    else
                    {
                        foreach (log_Format fmt in logformat.Values)
                        {
                            packettypecache[fmt.type] = new log_format_cache()
                            {
                                length = fmt.length,
                                type = fmt.type,
                                name = ASCIIEncoding.ASCII.GetString(fmt.name).Trim(new char[] { '\0' }),
                                format = ASCIIEncoding.ASCII.GetString(fmt.format).Trim(new char[] { '\0' }),
                            };

                            if (fmt.type == packettype)
                            {
                                name = packettypecache[fmt.type].name;
                                format = packettypecache[fmt.type].format;
                                size = fmt.length;
                                //break;
                            }
                        }
                    }

                    // didnt find a match, return unknown packet type
                    if (size == 0)
                        return;

                    br.Seek(size - 3, SeekOrigin.Current);
                    break;
            }
        }

        public
            object[] ReadMessageObjects(Stream br)
        {
            int log_step = 0;

            while (br.Position < br.Length)
            {
                byte data = (byte) br.ReadByte();

                switch (log_step)
                {
                    case 0:
                        if (data == HEAD_BYTE1)
                        {
                            log_step++;
                        }
                        break;

                    case 1:
                        if (data == HEAD_BYTE2)
                        {
                            log_step++;
                        }
                        else
                        {
                            log_step = 0;
                        }
                        break;

                    case 2:
                        log_step = 0;
                        try
                        {
                            var line = logEntryObjects(data, br);

                            return line;
                        }
                        catch
                        {
                            Debug.WriteLine("Bad Binary log line {0}", data);
                        }
                        break;
                }
            }

            return null;
        }

        object[] logEntryObjects(byte packettype, Stream br)
        {
            switch (packettype)
            {
                case 0x80: // FMT
                    ReadLogFormat(br);
                    return null;

                default:
                    string format = "";
                    string name = "";
                    int size = 0;

                    if (packettypecache.ContainsKey(packettype))
                    {
                        var fmt = packettypecache[packettype];
                        name = fmt.name;
                        format = fmt.format;
                        size = fmt.length;
                    }
                    else
                    {
                        foreach (log_Format fmt in logformat.Values)
                        {
                            packettypecache[fmt.type] = new log_format_cache()
                            {
                                length = fmt.length,
                                type = fmt.type,
                                name = ASCIIEncoding.ASCII.GetString(fmt.name).Trim(new char[] { '\0' }),
                                format = ASCIIEncoding.ASCII.GetString(fmt.format).Trim(new char[] { '\0' }),
                            };

                            if (fmt.type == packettype)
                            {
                                name = packettypecache[fmt.type].name;
                                format = packettypecache[fmt.type].format;
                                size = fmt.length;
                                //break;
                            }
                        }
                    }

                    // didnt find a match, return unknown packet type
                    if (size == 0)
                        return null;

                    byte[] data = new byte[size - 3]; // size - 3 = message - messagetype - (header *2)

                    br.Read(data, 0, data.Length);

                    return ProcessMessageObjects(data, name, format);
            }
        }

        private object[] ProcessMessageObjects(byte[] message, string name, string format)
        {
            char[] form = format.ToCharArray();

            int offset = 0;

            List<object> answer = new List<object>();

            foreach (char ch in form)
            {
                switch (ch)
                {
                    case 'b':
                        answer.Add((sbyte) message[offset]);
                        offset++;
                        break;
                    case 'B':
                        answer.Add(message[offset]);
                        offset++;
                        break;
                    case 'h':
                        answer.Add(BitConverter.ToInt16(message, offset));
                        offset += 2;
                        break;
                    case 'H':
                        answer.Add(BitConverter.ToUInt16(message, offset));
                        offset += 2;
                        break;
                    case 'i':
                        answer.Add(BitConverter.ToInt32(message, offset));
                        offset += 4;
                        break;
                    case 'I':
                        answer.Add(BitConverter.ToUInt32(message, offset));
                        offset += 4;
                        break;
                    case 'q':
                        answer.Add(BitConverter.ToInt64(message, offset));
                        offset += 8;
                        break;
                    case 'Q':
                        answer.Add(BitConverter.ToUInt64(message, offset));
                        offset += 8;
                        break;
                    case 'f':
                        answer.Add(BitConverter.ToSingle(message, offset));
                        offset += 4;
                        break;
                    case 'd':
                        answer.Add(BitConverter.ToDouble(message, offset));
                        offset += 8;
                        break;
                    case 'c':
                        answer.Add((BitConverter.ToInt16(message, offset)/100.0));
                        offset += 2;
                        break;
                    case 'C':
                        answer.Add((BitConverter.ToUInt16(message, offset)/100.0));
                        offset += 2;
                        break;
                    case 'e':
                        answer.Add((BitConverter.ToInt32(message, offset)/100.0));
                        offset += 4;
                        break;
                    case 'E':
                        answer.Add((BitConverter.ToUInt32(message, offset)/100.0));
                        offset += 4;
                        break;
                    case 'L':
                        answer.Add(((double) BitConverter.ToInt32(message, offset)/10000000.0));
                        offset += 4;
                        break;
                    case 'n':
                        answer.Add(ASCIIEncoding.ASCII.GetString(message, offset, 4).Trim(NullTerminator));
                        offset += 4;
                        break;
                    case 'N':
                        answer.Add(ASCIIEncoding.ASCII.GetString(message, offset, 16).Trim(NullTerminator));
                        offset += 16;
                        break;
                    case 'M':
                        int modeno = message[offset];
                        answer.Add(modeno);
                        offset++;
                        break;
                    case 'Z':
                        answer.Add(ASCIIEncoding.ASCII.GetString(message, offset, 64).Trim(NullTerminator));
                        offset += 64;
                        break;
                    default:
                        return null;
                }
            }
            return answer.ToArray();
        }

        /// <summary>
        /// Process each log entry
        /// </summary>
        /// <param name="packettype">packet type</param>
        /// <param name="br">input file</param>
        /// <returns>string of converted data</returns>
        string logEntry(byte packettype, Stream br)
        {
            switch (packettype)
            {
                case 0x80: // FMT

                    log_Format logfmt =  ReadLogFormat(br);

                    string lgname = ASCIIEncoding.ASCII.GetString(logfmt.name).Trim(NullTerminator);
                    string lgformat = ASCIIEncoding.ASCII.GetString(logfmt.format).Trim(NullTerminator);
                    string lglabels = ASCIIEncoding.ASCII.GetString(logfmt.labels).Trim(NullTerminator);
                    string line = String.Format("FMT, {0}, {1}, {2}, {3}, {4}\r\n", logfmt.type, logfmt.length, lgname,
                        lgformat, lglabels);

                    return line;

                default:
                    string format = "";
                    string name = "";
                    int size = 0;

                    if (packettypecache.ContainsKey(packettype))
                    {
                        var fmt = packettypecache[packettype];
                        name = fmt.name;
                        format = fmt.format;
                        size = fmt.length;
                    }
                    else
                    {
                        foreach (log_Format fmt in logformat.Values)
                        {
                            packettypecache[fmt.type] = new log_format_cache() {
                                length = fmt.length,
                                type = fmt.type,
                                name = ASCIIEncoding.ASCII.GetString(fmt.name).Trim(NullTerminator),
                                format = ASCIIEncoding.ASCII.GetString(fmt.format).Trim(NullTerminator),
                            };

                            if (fmt.type == packettype)
                            {
                                name = packettypecache[fmt.type].name;
                                format = packettypecache[fmt.type].format;
                                size = fmt.length;
                                //break;
                            }
                        }
                    }

                    // didnt find a match, return unknown packet type
                    if (size == 0)
                        return "UNKW, " + packettype;

                    byte[] data = new byte[size - 3]; // size - 3 = message - messagetype - (header *2)

                    br.Read(data, 0, data.Length);

                    return ProcessMessage(data, name, format);
            }
        }

        Dictionary<int, log_format_cache> packettypecache = new Dictionary<int, log_format_cache>();

        /*  
    105    +Format characters in the format string for binary log messages  
    106    +  b   : int8_t  
    107    +  B   : uint8_t  
    108    +  h   : int16_t  
    109    +  H   : uint16_t  
    110    +  i   : int32_t  
    111    +  I   : uint32_t  
    112    +  f   : float  
         *     d   : double
    113    +  N   : char[16]  
    114    +  c   : int16_t * 100  
    115    +  C   : uint16_t * 100  
    116    +  e   : int32_t * 100  
    117    +  E   : uint32_t * 100  
    118    +  L   : uint32_t latitude/longitude  
    119    + */


        /// <summary>
        /// Convert to ascii based on the existing format message
        /// </summary>
        /// <param name="message">raw binary message</param>
        /// <param name="name">Message type name</param>
        /// <param name="format">format string containing packet structure</param>
        /// <returns>formated ascii string</returns>
        string ProcessMessage(byte[] message, string name, string format)
        {
            char[] form = format.ToCharArray();

            int offset = 0;

            StringBuilder line = new StringBuilder(name,1024);

            foreach (char ch in form)
            {
                switch (ch)
                {
                    case 'b':
                        line.Append(", " + (sbyte) message[offset]);
                        offset++;
                        break;
                    case 'B':
                        line.Append(", " + message[offset]);
                        offset++;
                        break;
                    case 'h':
                        line.Append(", " +
                                    BitConverter.ToInt16(message, offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'H':
                        line.Append(", " +
                                    BitConverter.ToUInt16(message, offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'i':
                        line.Append(", " +
                                    BitConverter.ToInt32(message, offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'I':
                        line.Append(", " +
                                    BitConverter.ToUInt32(message, offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'q':
                        line.Append(", " +
                                    BitConverter.ToInt64(message, offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 8;
                        break;
                    case 'Q':
                        line.Append(", " +
                                    BitConverter.ToUInt64(message, offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 8;
                        break;
                    case 'f':
                        line.Append(", " +
                                    BitConverter.ToSingle(message,offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'd':
                        line.Append(", " +
                                    BitConverter.ToDouble(message, offset)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 8;
                        break;
                    case 'c':
                        line.Append(", " +
                                    (BitConverter.ToInt16(message, offset)/100.0).ToString("0.00",
                                        System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'C':
                        line.Append(", " +
                                    (BitConverter.ToUInt16(message, offset)/100.0).ToString("0.00",
                                        System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'e':
                        line.Append(", " +
                                    (BitConverter.ToInt32(message, offset)/100.0).ToString("0.00",
                                        System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'E':
                        line.Append(", " +
                                    (BitConverter.ToUInt32(message, offset)/100.0).ToString("0.00",
                                        System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'L':
                        line.Append(", " +
                                    ((double) BitConverter.ToInt32(message, offset)/10000000.0).ToString(
                                        System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'n':
                        line.Append(", " + ASCIIEncoding.ASCII.GetString(message, offset, 4).Trim(NullTerminator));
                        offset += 4;
                        break;
                    case 'N':
                        line.Append(", " + ASCIIEncoding.ASCII.GetString(message, offset, 16).Trim(NullTerminator));
                        offset += 16;
                        break;
                    case 'M':
                        int modeno = message[offset];
                        var modes = GetModesList();
                        string currentmode = "";

                        foreach (var mode in modes)
                        {
                            if (mode.Key == modeno)
                            {
                                currentmode = mode.Value;
                                break;
                            }
                        }

                        line.Append(", " + currentmode);
                        offset++;
                        break;
                    case 'Z':
                        line.Append(", " + ASCIIEncoding.ASCII.GetString(message, offset, 64).Trim(NullTerminator));
                        offset += 64;
                        break;
                    default:
                        return "Bad Conversion";
                }
            }

            line.Append("\r\n");
            return line.ToString();
        }

        List<KeyValuePair<int, string>> modeList;

        private List<KeyValuePair<int, string>> GetModesList()
        {
            if (modeList == null)
            {
                modeList = new List<KeyValuePair<int, string>>()
                {
                    new KeyValuePair<int, string>((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_MANUAL << 16, "Manual"),
                    new KeyValuePair<int, string>((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_ACRO << 16, "Acro"),
                    new KeyValuePair<int, string>((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_STABILIZED << 16, "Stabalized"),
                    new KeyValuePair<int, string>((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_RATTITUDE << 16, "Rattitude"),
                    new KeyValuePair<int, string>((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_ALTCTL << 16, "Altitude Control"),
                    new KeyValuePair<int, string>((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_POSCTL << 16, "Position Control"),
                    new KeyValuePair<int, string>((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_OFFBOARD << 16, "Offboard Control"),
                    new KeyValuePair<int, string>(
                        ((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_AUTO << 16) +
                        (int) PX4_CUSTOM_SUB_MODE_AUTO.PX4_CUSTOM_SUB_MODE_AUTO_READY << 24, "Auto: Ready"),
                    new KeyValuePair<int, string>(
                        ((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_AUTO << 16) +
                        (int) PX4_CUSTOM_SUB_MODE_AUTO.PX4_CUSTOM_SUB_MODE_AUTO_TAKEOFF << 24, "Auto: Takeoff"),
                    new KeyValuePair<int, string>(
                        ((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_AUTO << 16) +
                        (int) PX4_CUSTOM_SUB_MODE_AUTO.PX4_CUSTOM_SUB_MODE_AUTO_LOITER << 24, "Loiter"),
                    new KeyValuePair<int, string>(
                        ((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_AUTO << 16) +
                        (int) PX4_CUSTOM_SUB_MODE_AUTO.PX4_CUSTOM_SUB_MODE_AUTO_MISSION << 24, "Auto"),
                    new KeyValuePair<int, string>(
                        ((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_AUTO << 16) +
                        (int) PX4_CUSTOM_SUB_MODE_AUTO.PX4_CUSTOM_SUB_MODE_AUTO_RTL << 24, "RTL"),
                    new KeyValuePair<int, string>(
                        ((int) PX4_CUSTOM_MAIN_MODE.PX4_CUSTOM_MAIN_MODE_AUTO << 16) +
                        (int) PX4_CUSTOM_SUB_MODE_AUTO.PX4_CUSTOM_SUB_MODE_AUTO_LAND << 24, "Auto: Landing")
                };
            }
            return modeList;
        }

        enum PX4_CUSTOM_MAIN_MODE
        {
            PX4_CUSTOM_MAIN_MODE_MANUAL = 1,
            PX4_CUSTOM_MAIN_MODE_ALTCTL,
            PX4_CUSTOM_MAIN_MODE_POSCTL,
            PX4_CUSTOM_MAIN_MODE_AUTO,
            PX4_CUSTOM_MAIN_MODE_ACRO,
            PX4_CUSTOM_MAIN_MODE_OFFBOARD,
            PX4_CUSTOM_MAIN_MODE_STABILIZED,
            PX4_CUSTOM_MAIN_MODE_RATTITUDE
        }

        enum PX4_CUSTOM_SUB_MODE_AUTO
        {
            PX4_CUSTOM_SUB_MODE_AUTO_READY = 1,
            PX4_CUSTOM_SUB_MODE_AUTO_TAKEOFF,
            PX4_CUSTOM_SUB_MODE_AUTO_LOITER,
            PX4_CUSTOM_SUB_MODE_AUTO_MISSION,
            PX4_CUSTOM_SUB_MODE_AUTO_RTL,
            PX4_CUSTOM_SUB_MODE_AUTO_LAND,
            PX4_CUSTOM_SUB_MODE_AUTO_RTGS
        }
    }
}