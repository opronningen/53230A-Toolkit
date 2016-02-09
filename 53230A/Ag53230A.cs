using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;

namespace _53230A {
    public class Ag53230A {
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
                    if (i >= readBuffer.Length) {
                        byte[] tmp = new byte[readBuffer.Length * 2];
                        readBuffer.CopyTo(tmp, 0);
                        readBuffer = tmp;
                    }
                }
            } catch (IOException) {
                Console.WriteLine("Timeout.");
                Environment.Exit(-1);
            }

            return (Encoding.ASCII.GetString(readBuffer, 0, i));
        }

        // Retrieve a (set of) readings, following a Read, R or Data:Remove command.
        // Check configuration if it is real or ascii format
        public double[] GetReadings() {
            if (Conf.GetListByID(SettingID.form).SelectedItem().Equals("REAL,64")) {
                return ReadReals();
            } else {
                String str = ReadString().Trim();
                if (String.IsNullOrEmpty(str))
                    return null;

                String[] readings = str.Split(',');

                double[] values = new double[readings.Length];

                for (int i = 0; i < values.Length; i++)
                    if (!double.TryParse(readings[i], NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])) {
                        //Err.WriteLine("Could not parse '{0}'", readings[i]);
                    }

                return values;
            }
        }

        // Receive real-valued data in Definite-length Block Format
        public double[] ReadReals() {
            double[] readings = null;

            try {
                // Read first 8 bytes, first two is always '#n' if block format, where n is number of digits in number to follow.
                if (ns.Read(readBuffer, 0, 8) == -1)
                    return null;

                // If first character is #, interpret as Block Form. Else parse as single value.
                if (readBuffer[0] == '#') {

                    // "Number of digits in the number of bytes" - typically 2-4. Absolute worst case is 8000000
                    // (1 million readings), 7 digits. Will fail.. 6 digits (<125k readings) will be ok.
                    int numdigits = 0;
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

                    // Read actual datablock
                    if (ns.Read(readBuffer, 2 + numdigits, numbytes) == -1)
                        return null;

                    // allocate an array for the readings.
                    readings = new double[numbytes / 8];

                    for (int i = 0; i < readings.Length; i++)
                        readings[i] = BitConverter.ToDouble(readBuffer, (i * 8) + numdigits + 2);

                } else {

                    // Single reading returned

                    readings = new double[1];
                    readings[0] = BitConverter.ToDouble(readBuffer, 0);
                }
            } catch (IOException) {
                Console.WriteLine("Timeout.");
                Environment.Exit(-1);
            }

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
            StreamReader sr = new StreamReader("Ag53230A.ini");

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
