using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace _53230A {
    public class Ag53230A {
        public bool debug = false;

        public Configuration Conf = new Configuration();

        public bool AutoCommit = true;      // Immediately send configuration-statements to the instrument when changes are made.
                                            // If many changes are to be made, set AutoCommit false, and call CommitConfig() when all
                                            // Changes has been made.

        byte[] readBuffer = new byte[1024 * 1024];
        byte[] sendBuffer = new byte[1024];

        NetworkStream ns;
        TcpClient t;

        public void WriteString(String s) {
            if (!s.EndsWith("\n"))
                s = s + "\n";

            sendBuffer = Encoding.ASCII.GetBytes(s);
            ns.Write(sendBuffer, 0, s.Length);
        }

        public string ReadString() {
            int res = 0;
            int i = 0;

            if (!ns.CanRead)
                return (null);

            // Stop reading when we get CR/LF
            try {
                while (res != 10) {
                    res = ns.ReadByte();
                    if (res == -1)
                        break;

                    readBuffer[i++] = (byte)res;

                    // Check for buffer overflow - double the readbuffer size
                    while (i >= readBuffer.Length) {
                        byte[] tmp = new byte[readBuffer.Length * 2];
                        readBuffer.CopyTo(tmp, 0);
                        readBuffer = tmp;
                    }
                }
            } catch (IOException) {
                Console.Error.WriteLine("Timeout.");
                Environment.Exit(-1);
            }

            return (Encoding.ASCII.GetString(readBuffer, 0, i));
        }

        // Retrieve a (set of) readings, following a Read, R or Data:Remove command.
        // Check configuration if it is real or ascii format
        public double[] GetReadings() {
            if (Conf.GetListByID(SettingID.form).SelectedItem().Equals("REAL,64")) {
                if (debug)
                    Console.Error.WriteLine("Receiving REAL64 data");

                return ReadReals();
            } else {
                if (debug)
                    Console.Error.WriteLine("Receiving ASCII data");

                String str = ReadString().Trim();
                if (String.IsNullOrEmpty(str))
                    return null;

                String[] readings = str.Split(',');

                double[] values = new double[readings.Length];

                for (int i = 0; i < values.Length; i++)
                    values[i] = double.Parse(readings[i], NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture);

                if (debug)
                    Console.Error.WriteLine("Received {0} measurements", values.Length);

                return values;
            }
        }

        // Receive real-valued data in Definite-length Block Format
        public double[] ReadReals() {
            double[] readings = null;

            //readBuffer.Initialize();
            for (int i = 0; i < readBuffer.Length; i++)
                readBuffer[i] = 0;
     
            try {

                // Read first 8 bytes, first two is always '#n' if block format, where n is number of digits in number to follow.
                int n = 0;
                int bytesToRead = 8;
                int offset = 0;
                while(bytesToRead > 0) {
                    n = ns.Read(readBuffer, offset, bytesToRead);
                    if (n == 0)
                        throw new Exception("Instrument closed connection.");

                    bytesToRead -= n;
                    offset += n;
                }

                // If first character is #, interpret as Block Form. Else parse as single value.
                if (readBuffer[0] == '#') {
                    int numdigits = 0;

                    // If next characters is 0, interpret as indefinite block length, terminated by \10
                    // Else parse as definite block length
                    if (readBuffer[1] == '0') {

                        // Read number of samples configured
                        int samplecount = (int)Conf.GetNumericByID(SettingID.samp_coun).value;

                        // Make sure buffer is large enough
                        while(2 + (samplecount * 8) > readBuffer.Length) {
                            if (debug)
                                 Console.Error.WriteLine("Increasing receive-buffer from {0} to {1} bytes", readBuffer.Length, readBuffer.Length * 2);

                            byte[] tmp = new byte[readBuffer.Length * 2];
                            Array.Copy(readBuffer, tmp, 8);
                            readBuffer = tmp;
                        }

                        bytesToRead = (samplecount * 8) - 6 + 1;
                        while (bytesToRead > 0) {
                            n = ns.Read(readBuffer, offset, bytesToRead);
                            if (n == 0)
                                throw new Exception("Instrument closed connection.");

                            bytesToRead -= n;
                            offset += n;
                        }

                        //if(debug)
                        //    Console.Error.WriteLine("Indefinite Length Block data");

                        //numbytes = 6;   // Subtract '#0'

                        //// Indefinite block lenght:
                        //// 2 byte header, at least 2 8-byte measurements, total 18 bytes. Terminator in 19th, or n*8 bytes later.
                        //// Read another 11 bytes

                        //n = ns.Read(readBuffer, offset, 11);
                        //if(n != 11) {
                        //    Console.Error.WriteLine("Error! Expected 11 bytes, got {0}", n);
                        //}

                        //offset += n;

                        //// Read 8 bytes at a time, checking for terminator in last position
                        //while(readBuffer[offset-1] != 10){
                        //    n = ns.Read(readBuffer, offset, 8);
                        //    if (n != 8) {
                        //        Console.Error.WriteLine("Error! Expected 8 bytes, got {0}", n);

                        //        break;
                        //    }

                        //    offset += n;

                        //    // Grow buffer if needed
                        //    while (offset+8 >= readBuffer.Length) {
                        //        if (debug)
                        //            Console.Error.WriteLine("Increasing receive-buffer from {0} to {1} bytes", readBuffer.Length, readBuffer.Length * 2);

                        //        byte[] tmp = new byte[readBuffer.Length * 2];
                        //        Array.Copy(readBuffer, tmp, 8);
                        //        readBuffer = tmp;
                        //    }
                        //} 

                        if (debug)
                            Console.Error.WriteLine("Got terminator, read {0} bytes", offset);

                    } else {

                        if(debug)
                            Console.Error.WriteLine("Definite Length Block data");

                        // "Number of digits in the number of bytes" - typically 2-4. Absolute worst case is 8000000
                        // (1 million readings), 7 digits. Will fail.. 6 digits (<125k readings) will be ok.    
                        if (!Int32.TryParse(Encoding.ASCII.GetString(readBuffer, 1, 1), out numdigits))
                            return null;

                        // numBytes contain actual number of bytes the counter returns
                        int numbytes = 0;
                        if (!Int32.TryParse(Encoding.ASCII.GetString(readBuffer, 2, numdigits), out numbytes))
                            return null;

                        // Ensure we have enough space to receive the block, account for header
                        while (numbytes + numdigits + 2 > readBuffer.Length) {
                            byte[] tmp = new byte[readBuffer.Length * 2];
                            Array.Copy(readBuffer, tmp, 8);
                            readBuffer = tmp;
                        }

                        // Read rest of datablock - we already gobbled up 8 bytes 
                        // Account for Definite Block Length - in Indefinite Block Length headers
                        // + 1. \10 is the last value.
                        bytesToRead = numbytes - (8 - 2 - numdigits) + 1;
                        offset = 8;
                        while (bytesToRead > 0) {
                            n = ns.Read(readBuffer, offset, bytesToRead);
                            if (n < 1)
                                throw new Exception("Instrument closed connection.");

                            bytesToRead -= n;
                            offset += n;
                        }
                    }

                    // allocate an array for the readings.
                    readings = new double[(offset - numdigits - 2) / 8];

                    for (int i = 0; i < readings.Length; i++)
                        readings[i] = BitConverter.ToDouble(readBuffer, (i * 8) + numdigits + 2);
                    
                } else {

                    // Single reading returned

                    readings = new double[1];
                    readings[0] = BitConverter.ToDouble(readBuffer, 0);
                }
            } catch (IOException) {
                Console.Error.WriteLine("Timeout.");
                Environment.Exit(-1);
            }

            if (debug)
                Console.Error.WriteLine("Received {0} measurements", readings.Length);

            return readings;
        }

        // Learn the current (relevant) settings of the counter
        public void LearnConfig() {
            WriteString("Abort;*WAI;*LRN?");
            string settings = ReadString().TrimEnd();

            Conf.LearnConfig(settings);
        }

        // Upload new configuration to the instrument - not needed if AutoCommit == true
        public void CommitConfig() {
            WriteString("Abort");

            foreach (string stmt in Conf.BuildDeltaConfig())
                WriteString(stmt);
        }

        public string[] ReadErrors() {

            List<string> errors = new List<string>();

            string s;
            while (true) {
                WriteString(";SYST:ERR?");
                s = ReadString().Trim();

                if (!s.StartsWith("+0,"))
                    errors.Add(s);
                else
                    break;
            }

            return errors.ToArray();
        }

        public Ag53230A() {
            // Look for ini-file in the directory where the executable is located.
            string path = System.Reflection.Assembly.GetEntryAssembly().CodeBase;
            var directory = Path.GetDirectoryName(path);
            string fileuri = Path.Combine(directory, "Ag53230A.ini");
            StreamReader sr = new StreamReader(new Uri(fileuri).LocalPath);

            if (true)
                Console.Error.WriteLine("Using config-file {0}", fileuri);

            string s, host = "";
            int timeout = 100;
            string[] keyVal;

            while ((s = sr.ReadLine()) != null) {
                keyVal = s.Split(new char[] { '=', ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (keyVal[0].Equals("address", StringComparison.InvariantCultureIgnoreCase))
                    host = keyVal[1];
                else if (keyVal[0].Equals("timeout", StringComparison.InvariantCultureIgnoreCase))
                    timeout = Int32.Parse(keyVal[1]);
            }

            t = new TcpClient(host, 5025);
            ns = t.GetStream();
            ns.ReadTimeout = timeout;
        }

        ~Ag53230A() {
            t.Close();
        }
    }
}
