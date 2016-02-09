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

            instr.WriteString("READ?");

            String str = instr.ReadString();

            String[] readings = str.Split(new char[] { ',' });

            foreach(string r in readings)
                Console.WriteLine(r);

            string[] errors = instr.ReadErrors();
            if (errors.Length > 1)
                foreach (string error in errors)
                    Console.Error.WriteLine(error);
        }
    }
}
