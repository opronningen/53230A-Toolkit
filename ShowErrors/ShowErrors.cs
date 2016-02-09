using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _53230A;

namespace ShowErrors {
    class ShowErrors {
        static void Main(string[] args) {
            Ag53230A instr = new Ag53230A();

            string[] errors = instr.ReadErrors();
            foreach (string error in errors)
                Console.Error.WriteLine(error);
        }
    }
}
