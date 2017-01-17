using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _53230A;

namespace _53230A_Read {
   
    class Read {
        static void Main(string[] args) {
            
            Ag53230A instr = new Ag53230A();
            instr.LearnConfig();

            int repeat = -1;

            if(args.Length > 0)
                if (!Int32.TryParse(args[0], out repeat))
                    Console.Error.WriteLine("Warning! Unable to parse argument {0}", args[0]);

            while (repeat == -1 || repeat-- > 0) {
                instr.WriteString("READ?");

                double[] res = instr.GetReadings();

                foreach (double d in res) {
                    Console.WriteLine(d.ToString());    // Todo: formatstring
                }

                //String str = instr.ReadString().Trim();

                //String[] readings = str.Split(new char[] { ',' });

                //foreach (string r in readings)
                //    Console.WriteLine(r);
            }

            string[] errors = instr.ReadErrors();
            foreach (string error in errors)
                Console.Error.WriteLine(error);
        }
    }
}
