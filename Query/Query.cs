using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _53230A;

namespace Query {
    class Query {
        static void Main(string[] args) {
            Ag53230A instr = new Ag53230A();

            if (args.Length > 0) {
                instr.WriteString(args[0]);
                if (!args[0].Contains("?")){
                    Console.WriteLine("Not a query.");     // Queries usually ends with ?, but at least contains ? ("TRIG:SOUR?", ":DATA:REM? 1,WAIT")
                    return;
                }

            } else
                return;

            String str = instr.ReadString();

            String[] res = str.Split(new char[] { ',' });

            foreach (string r in res)
                Console.WriteLine(r);

            // Write errorlog - if any.
            string[] errors = instr.ReadErrors();
            foreach (string error in errors)
                Console.Error.WriteLine(error);
        }
    }
}
