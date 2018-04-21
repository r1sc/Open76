using Assets.Scripts.System;
using System;
using System.IO;
using System.Linq;

namespace i76dasm
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                ShowHelp();
                return;
            }

            var command = args[0];
            if (command == "-d" && args.Length == 3)
            {
                var msnPath = args[1];
                var outPath = args[2];
                var fsm = MsnFSMParser.ReadMission(msnPath);
                DisassembleTo(fsm, outPath);
            }
            else if (command == "-a" && args.Length == 4)
            {
                var inputTxtPath = args[1];
                var inputMsnPath = args[2];
                var outputMsnPath = args[3];
                Assemble(inputTxtPath, inputMsnPath, outputMsnPath);
            }
            else
            {
                ShowHelp();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("i76dasm.exe - Reads/Writes Interstate 76 mission file scripts");
            Console.WriteLine("https://github.com/r1sc/Open76");
            Console.WriteLine();
            Console.WriteLine("usage: i76dasm.exe <command> <commandoptions>");
            Console.WriteLine(" -d <input.msn> <output.txt>                Disassemble mission-file to text-file");
            Console.WriteLine(" -a <input.txt> <input.msn> <output.msn>    Assemble text-file and injects into mission-file");
        }

        static void Assemble(string inputTxtPath, string inputMsnPath, string outputMsnPath)
        {
            var fsm = MsnFSMParser.AssembleFSM(inputTxtPath);
            MsnFSMParser.WriteMission(inputMsnPath, fsm, outputMsnPath);
        }

        static void DisassembleTo(FSM fsm, string txtPath)
        {
            using (var sw = new StreamWriter(txtPath))
            {
                sw.WriteLine("section actions");
                for (int i = 0; i < fsm.ActionTable.Length; i++)
                {
                    sw.WriteLine(i.ToString() + ": " + fsm.ActionTable[i]);
                }
                sw.WriteLine();

                sw.WriteLine("section soundclips");
                for (int i = 0; i < fsm.SoundClipTable.Length; i++)
                {
                    sw.WriteLine(i.ToString() + ": " + fsm.SoundClipTable[i]);
                }
                sw.WriteLine();

                sw.WriteLine("section entities");
                var entityIndex = 0;
                foreach (var entityKeyValue in fsm.EntityTable)
                {
                    sw.WriteLine(entityIndex.ToString() + ": " + entityKeyValue.Label + " = " + entityKeyValue.Value);
                    entityIndex++;
                }
                sw.WriteLine();

                sw.WriteLine("section paths");
                for(var i = 0; i < fsm.Paths.Count; i++)
                {
                    sw.WriteLine(i.ToString() + ": " + fsm.Paths[i].Name + ", " + string.Join(", ", fsm.Paths[i].Nodes.Select(n => n.ToString())));
                }
                sw.WriteLine();

                sw.WriteLine("section data");
                for (var i = 0; i < fsm.Constants.Length; i++)
                {
                    sw.WriteLine(i.ToString() + ": " + fsm.Constants[i]);
                }
                sw.WriteLine();

                sw.WriteLine("section machines");
                for (var i = 0; i < fsm.StackMachines.Count; i++)
                {
                    var sm = fsm.StackMachines[i];
                    sw.WriteLine(i.ToString() + ": " + sm.StartAddress + ", " + string.Join(", ", sm.InitialArguments.Select(x => x.ToString()).ToArray()));
                }
                sw.WriteLine();

                sw.WriteLine("section code");
                for (int i = 0; i < fsm.ByteCode.Length; i++)
                {
                    var byteCode = fsm.ByteCode[i];

                    var value = byteCode.Value.ToString();
                    var line = i.ToString() + ": ";
                    if (byteCode.OpCode == OpCode.ACTION)
                    {
                        line += "action";
                        value = fsm.ActionTable[byteCode.Value];
                    }
                    else
                        line += byteCode.ToString();

                    line = line.PadRight(20) + " " + value;

                    sw.WriteLine(line);
                }
            }
        }
    }
}
