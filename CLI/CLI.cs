using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using _53230A;

namespace CLI {
    class CLI {
        static Ag53230A instr = new Ag53230A();

        /*
        "novel" cmdline completion:
        1. Read keypress.
        2. Retrieve candidate SCPInodes.
        2a. If only one candidate, complete current keyword
        2b. If single candidate is SCPInode (not Setting), append :
        2c. If single candidate is Setting, append " "
        2d. If no candidates, beep
        6. Goto 1
        */
        static void Main(string[] args) {
            List<string> prevCmdLines = new List<string>();

            string prompt = "53230A> ";
            StringBuilder currCmdLine = new StringBuilder();

            int prevCmdLineIndex = 0;       // Count up/down arrows for cmdline editing
            int currCmdLineIndex = 0;       // Count left/right arrows for cmdline editing

            bool useAutocomplete = true;    // 
            bool atSetting = false;         // Flag to signify if we are on a Setting-object. Note, a setting-object may still have children..

            string currKeyword = "";

            SCPInode currentNode = instr.Conf.root;

            Console.Write("{0}{1}", prompt, currCmdLine);

            ConsoleKeyInfo k;
            while (true) {
                k = Console.ReadKey(true);

                switch (k.Key){

                    // If tab, print a list of all possible keywords
                    case ConsoleKey.Tab:
                        Console.WriteLine();

                        foreach (SCPInode node in currentNode.Children.FindAll(s => s.Name.StartsWith(currKeyword, StringComparison.InvariantCultureIgnoreCase)))
                            Console.WriteLine(node.Name);

                        Console.Write("{0}{1}", prompt, currCmdLine);

                        break;

                    case ConsoleKey.UpArrow:
                        break;

                    case ConsoleKey.DownArrow:
                        break;

                    case ConsoleKey.LeftArrow:
                        break;

                    case ConsoleKey.RightArrow:
                        break;

                    case ConsoleKey.End:
                        break;

                    case ConsoleKey.Home:
                        break;

                    case ConsoleKey.Backspace:

                        // Remove one character at a time from currKeyword
                        // when currKeyword is "", remove one complete keyword at
                        // a time, "walking back up the parsetree", until currentNode.Parent == null.
                        
                        break;

                    // Send current command line, reset variables
                    case ConsoleKey.Enter:
                        prevCmdLineIndex = 0;
                        currCmdLineIndex = 0;

                        currCmdLine.Append(currKeyword);
                        Console.WriteLine();
                        Console.WriteLine(currCmdLine.ToString());

                        //instr.WriteString(currCmdLine.ToString());
                        // Read results

                        currentNode = instr.Conf.root;
                        currCmdLine.Clear();
                        break;

                    // If space, and we are at a Setting-node, autocomplete should 
                    // propose valid settings, rather than child-nodes..
                    // If we are not at a ListSetting-object, all bets are off, and anything goes. No autocomplete
                    case ConsoleKey.Spacebar:
                        if (useAutocomplete) {
                            // If there are bytes in currKeyword, it may be wrong. What to do?
                            if (currKeyword.Length != 0) {
                                currCmdLine.Append(currKeyword);
                                currCmdLine.Append(k.KeyChar);
                                currKeyword = "";
                            }

                            if (currentNode is ListSetting) {
                                atSetting = true;       // Autocomplete now looks for valid values in the ListSetting object
                            } else {
                                useAutocomplete = false;
                            }
                        } else {
                            Console.Write(k.KeyChar);
                            currKeyword += k.KeyChar;
                        }
                        break;

                    // Default; print char
                    default:
                        if (useAutocomplete) {
                            if (atSetting) {
                                if(currentNode is ListSetting) {
                                    ListSetting s = currentNode as ListSetting;
                                    List<string> strings = (new List<string>(s.AllowableValues)).FindAll
                                        (v => v.StartsWith(currKeyword + k.KeyChar, StringComparison.InvariantCultureIgnoreCase));

                                    if(strings.Count == 0) {
                                        Console.Beep();
                                        useAutocomplete = false;
                                        currKeyword += k.KeyChar;
                                    }else if(strings.Count == 1) {

                                    } else {
                                        currKeyword += k.KeyChar;
                                    }
                                } else {
                                    currKeyword += k.KeyChar;
                                }
                                
                            } else {
                                List<SCPInode> nodes = currentNode.Children.FindAll(s => s.Name.StartsWith(currKeyword + k.KeyChar, StringComparison.InvariantCultureIgnoreCase));
                                if (nodes.Count == 0) {
                                    // An invalid character has been entered. Turn off autocomplete, and continue gobbling up
                                    // characters? There may be valid queries that is not reflected in the Config-object.
                                    // Turn autocomplete back on if backspace is entered enough times to get to a place where 
                                    // a valid character can be entered? Room for mistakes, what if : has been entered?

                                    Console.Beep();
                                    useAutocomplete = false;
                                    currKeyword += k.KeyChar;
                                } else if (nodes.Count == 1) {
                                    currentNode = nodes.First();
                                    currCmdLine.Append(":");
                                    currCmdLine.Append(currentNode.Name);

                                    // Step left number of bytes already accepted in current keyword, overwrite
                                    Console.CursorLeft = Console.CursorLeft - currKeyword.Length;
                                    Console.Write(":{0}", currentNode.Name);

                                    // If currentNode has children, append : to signal this. If not, append ' '
                                    if (currentNode.Children.Count != 0) {
                                        Console.Write(":");
                                    } else {
                                        Console.Write(" ");
                                        atSetting = true;
                                    }

                                    currKeyword = "";
                                } else {
                                    currKeyword += k.KeyChar;
                                }
                            }
                        } else {
                            Console.Write(k.KeyChar);
                            currKeyword += k.KeyChar;
                        }

                        break;

                }
            }
            
        }
    }
}
