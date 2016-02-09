using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        * Runs untill aborted with ctrl-c.
        *
        * To do:
        *   Add option to NOT sent INIT:IMM, if instrument gets triggered from some other source.
        *   Add option to specify number of samples to retrieve per call. Reduce number of IO for fast measurements.
        *   Add option to specify timeout
        *   Add option to specify number of readings to fetch in total, then exit.
        *   Add functionality to retrieve results in other formats; as specified with ":FORMat:DATA", definite block length
        *
        */

        static void Main(string[] args)
        {

            Ag53230A instr = new Ag53230A();

            int pts = 1;    // Default retrieve 1 pt per call
            if (args.Length != 0) {
                if (!Int32.TryParse(args[0], out pts)) {
                    Console.WriteLine("Could not parse parameter '{0}'", args[0]);
                    return;
                }
            }

            // Trigger
            instr.WriteString("ABORT;*WAI;INIT:IMM");
            System.Threading.Thread.Sleep(20);

            string query = String.Format(":DATA:REMOVE? {0},WAIT", pts.ToString());

            while (true)
            {
                instr.WriteString(query);

                String str = instr.ReadString().Trim();
                if (String.IsNullOrEmpty(str))
                    break;

                String[] readings = str.Split(new char[] { ',' });

                foreach (string r in readings)
                    Console.WriteLine(r);
            }
        }
    }
}
