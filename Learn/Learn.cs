using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _53230A;

namespace Learn {
    class Learn {

#region Default instrument settings. 
        // This is the instrument configuraton immedately following a *RST. No need to display these, if the settings are still the same.
        static string[] Defaultsettings = new string[]{   
            ":FUNC \"FREQ\"",
            ":CALC:AVER 0",
            ":CALC:LIM:LOW +0.00000000000000E+000",
            ":CALC:LIM 0",
            ":CALC:LIM:UPP +0.00000000000000E+000",
            ":CALC:SCAL:FUNC NULL",
            ":CALC:SCAL:GAIN +1.00000000000000E+000",
            ":CALC:SCAL:INV 0",
            ":CALC:SCAL:OFFS +0.00000000000000E+000",
            ":CALC:SCAL:REF +0.00000000000000E+000",
            ":CALC:SCAL:REF:AUTO 1",
            ":CALC:SCAL 0",
            ":CALC:SCAL:UNIT \"\"",
            ":CALC:SCAL:UNIT:STAT 0",
            ":CALC:SMO:RESP FAST",
            ":CALC:SMO 0",
            ":CALC 0",
            ":CALC2:TRAN:HIST:POIN +100",
            ":CALC2:TRAN:HIST:RANG:LOW +0.00000000000000E+000",
            ":CALC2:TRAN:HIST:RANG:UPP +0.00000000000000E+000",
            ":CALC2:TRAN:HIST:RANG:AUTO:COUN +100",
            ":CALC2:TRAN:HIST:RANG:AUTO 1",
            ":CALC2:TRAN:HIST 0",
            ":CAL:VAL +0.00000000000000E+000",
            ":DATA:POIN:EVEN:THR +1",
            ":FORM:BORD NORM",
            ":FORM ASC,15",
            ":HCOP:SDUM:DATA:FORM PNG",
            ":INP:COUP AC",
            ":INP:FILT 0",
            ":INP:IMP +1.00000000E+006",
            ":INP:LEV +0.00000000E+000",
            ":INP:LEV:REL +50",
            ":INP:LEV2 +0.00000000E+000",
            ":INP:LEV2:REL +50",
            ":INP:LEV:AUTO 1",
            ":INP:NREJ 0",
            ":INP:PROB +1",
            ":INP:RANG +5.00000000E+000",
            ":INP:SLOP POS",
            ":INP:SLOP2 POS",
            ":INP2:COUP AC",
            ":INP2:FILT 0",
            ":INP2:IMP +1.00000000E+006",
            ":INP2:LEV +0.00000000E+000",
            ":INP2:LEV:REL +50",
            ":INP2:LEV2 +0.00000000E+000",
            ":INP2:LEV2:REL +50",
            ":INP2:LEV:AUTO 1",
            ":INP2:NREJ 0",
            ":INP2:PROB +1",
            ":INP2:RANG +5.00000000E+000",
            ":INP2:SLOP POS",
            ":INP2:SLOP2 POS",
            ":INP3:BURS:LEV -6.00000000E+000",
            ":MMEM:CDIR \"INT:\\\"",
            ":OUTP:POL NORM",
            ":OUTP 0",
            ":SAMP:COUN +1",
            ":FREQ:BURS:GATE:AUTO 1",
            ":FREQ:BURS:GATE:DEL +0.00000000000000E+000",
            ":FREQ:BURS:GATE:NARR 0",
            ":FREQ:BURS:GATE:TIME +1.00000000000000E-006",
            ":FREQ:GATE:POL NEG",
            ":FREQ:GATE:SOUR TIME",
            ":FREQ:GATE:TIME +1.00000000000000E-001",
            ":FREQ:MODE AUTO",
            ":GATE:EXT:SOUR BNC",
            ":GATE:STAR:DEL:EVEN +1",
            ":GATE:STAR:DEL:SOUR IMM",
            ":GATE:STAR:DEL:TIME +0.00000000000000E+000",
            ":GATE:STAR:SLOP NEG",
            ":GATE:STAR:SOUR IMM",
            ":GATE:STOP:HOLD:EVEN +1",
            ":GATE:STOP:HOLD:SOUR IMM",
            ":GATE:STOP:HOLD:TIME +0.00000000000000E+000",
            ":GATE:STOP:SLOP POS",
            ":GATE:STOP:SOUR IMM",
            ":TINT:GATE:POL NEG",
            ":TINT:GATE:SOUR IMM",
            ":TOT:GATE:POL NEG",
            ":TOT:GATE:SOUR TIME",
            ":TOT:GATE:TIME +1.00000000000000E-001",
            ":TST:RATE +1.00000000E+006",
            ":TRIG:COUN +1",
            ":TRIG:DEL +0.00000000000000E+000",
            ":TRIG:SLOP NEG",
            ":TRIG:SOUR IMM"
        };
#endregion

        static void Main(string[] args) {
            Ag53230A instr = new Ag53230A();

            
            List<string> input = new List<string>();

            // If input is redirected, assume it is from a file we want to upload to the instrument.
            if (Console.IsInputRedirected) {
                input.Add(Console.In.ReadToEnd());
            }

            // If arguments are given, assume it is strings to be sent to the instrument
            input.AddRange(args);

            // Send whatever we got.
            foreach(string s in input){
                string[] stmts = s.Split(new char[] { ';', '\n' });

                foreach (string stmt in stmts)
                    instr.WriteString(stmt.Trim());
            }


            // If no input, get current configuration state from instrument
            if(input.Count == 0) {
                instr.WriteString("*LRN?");
                string s = instr.ReadString();
                s = s.Trim();
                string[] stmts = s.Split(new char[] { ';' });

                // Filter out default settings
                foreach (string stmt in stmts)
                    if (!Defaultsettings.Contains(stmt))
                        Console.WriteLine(stmt + ";");
            }


            // If errors, print.
            string[] errors = instr.ReadErrors();
            if (errors.Length > 1)
                foreach (string error in errors)
                    Console.Error.WriteLine(error);
        }
    }
}
