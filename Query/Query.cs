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

            if (args.Length > 0)
                instr.WriteString(args[0]);
            else
                return;

            String str = instr.ReadString();

            String[] res = str.Split(new char[] { ',' });

            foreach (string r in res)
                Console.WriteLine(r);

            // Write errorlog - if any. Last errormessage is always +0, "No Error"
            string[] errors = instr.ReadErrors();
            foreach (string error in errors)
                Console.Error.WriteLine(error);
        }
    }
}
