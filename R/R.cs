using System;
using System.Globalization;
using System.IO;
using _53230A;

namespace R
{
    class R
    {
        /*
        * Triggers a measurement and retrieves results while measurements are ongoing. 
        * The instrument will continue to make measurements untill the number of measurements
        * specified with ":samp:count" has been made, possibly multiplied with the number of triggers
        * to accept, specified with ":trig:count". Depends on the measurement mode, RTFM.
        * 
        * Runs untill aborted with ctrl-c, or times out.
        *
        * To do:
        *   -i Add option to NOT sent INIT:IMM, if instrument gets triggered from some other source.
        *   -c Add option to specify number of readings to fetch in total, then exit.
        *   -b Add option to specify a command to send before starting the acquisition -"ABORT;:TRIG:SOUR BUS;*TRG"
        *   -a Add option to specify a command to send before every "R?" - "*TRG"
        *   -v Add option verbose, show number of measurements read
        *   -p Option number of points to receive each call
        *   -l do not learn instrument config?
        *   -h Help
        *
        */

        static void Main(string[] args)
        {
            Ag53230A instr = new Ag53230A();
            instr.LearnConfig();

            StreamWriter err = new StreamWriter(Console.OpenStandardError());
            err.AutoFlush = true;
          
            int pts = 1;    // Default retrieve 1 pt per call
            if (args.Length != 0) {
                if (!Int32.TryParse(args[0], out pts)) {
                    Console.WriteLine("Could not parse parameter '{0}'", args[0]);
                    return;
                }
            }

            double[] readings;

            // Trigger
            instr.WriteString("ABORT;*WAI;INIT:IMM");
            System.Threading.Thread.Sleep(20);                  // The instrument will beep if the :DATA:REM follows too fast after INIT:IMM

            string query = String.Format(":DATA:REMOVE? {0},WAIT", pts.ToString());

            instr.WriteString("*TRG");
            while (true)
            {

                instr.WriteString("*TRG");
                instr.WriteString(query);

                readings = instr.GetReadings();

                if (readings.Length != pts)
                    err.WriteLine("Warning: Expected {0} readings, received {1}.", pts, readings.Length);

                foreach (double d in readings)
                    Console.WriteLine(d.ToString("E15", CultureInfo.InvariantCulture));
                /*
                String str = instr.ReadString().Trim();
                if (String.IsNullOrEmpty(str))
                    break;

                String[] readings = str.Split(new char[] { ',' });

                foreach (string r in readings)
                    Console.WriteLine(r);
                */
            }
        }
    }
}
