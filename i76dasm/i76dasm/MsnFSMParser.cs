using Assets.Fileparsers;
using Assets.Scripts.I76Types;
using Assets.Scripts.System;
using Assets.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace i76dasm
{
    class MsnFSMParser
    {
        class TagChunk
        {
            public string TagName { get; set; }
            public byte[] Data { get; set; }
        }

        private static string TrimLineNumber(string line)
        {
            var colonStart = line.IndexOf(':');
            if (colonStart == -1)
                return line;
            return line.Substring(colonStart+1);
        }

        public static FSM AssembleFSM(string inputTxtPath)
        {
            var actions = new List<string>();
            var soundclips = new List<string>();
            var paths = new List<FSMPath>();
            var entities = new List<FSMEntity>();
            var datas = new List<int>();
            var machines = new List<StackMachine>();
            var code = new List<ByteCode>();

            using(var sr = new StreamReader(inputTxtPath))
            {
                var section = "";
                while (!sr.EndOfStream)
                {
                    var line = TrimLineNumber(sr.ReadLine().Trim()).Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("section"))
                    {
                        section = line.Substring("section".Length + 1).Trim();
                    }
                    else
                    {
                        switch (section)
                        {
                            case "actions":
                                actions.Add(line);
                                break;
                            case "soundclips":
                                soundclips.Add(line);
                                break;
                            case "paths":
                                var pathValues = line.Split(',');
                                var name = pathValues[0].Trim();
                                var nodes = new List<I76Vector3>();

                                for (int i = 1; i < pathValues.Length; i+=3)
                                {
                                    nodes.Add(new I76Vector3(float.Parse(pathValues[i]), float.Parse(pathValues[i + 1]), float.Parse(pathValues[i + 2])));
                                }

                                paths.Add(new FSMPath
                                {
                                    Name = name,
                                    Nodes = nodes.ToArray()
                                });
                                break;
                            case "entities":
                                var keyValue = line.Split('=');
                                entities.Add(new FSMEntity
                                {
                                    Label = keyValue[0].Trim(),
                                    Value = keyValue[1].Trim()
                                });
                                break;
                            case "data":
                                datas.Add(int.Parse(line));
                                break;
                            case "machines":                                
                                var values = line.Split(',');
                                var startAddr = uint.Parse(values[0]);
                                var args = new List<int>();

                                if (values.Length > 1)
                                {
                                    for (int i = 1; i < values.Length; i++)
                                    {
                                        args.Add(int.Parse(values[i]));
                                    }
                                }
                                machines.Add(new StackMachine
                                {
                                    StartAddress = startAddr,
                                    InitialArguments = args.ToArray()
                                });
                                break;
                            case "code":
                                var opcodeValue = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var opcode = (OpCode)Enum.Parse(typeof(OpCode), opcodeValue[0], true);

                                int value;
                                if(opcode == OpCode.ACTION)
                                {
                                    value = actions.IndexOf(opcodeValue[1]);
                                }
                                else
                                {
                                    value = int.Parse(opcodeValue[1]);
                                }

                                code.Add(new ByteCode
                                {
                                    OpCode = opcode,
                                    Value = value
                                });
                                break;
                            default:
                                throw new Exception("Unknown section " + section);
                        }
                    }
                }
            }

            return new FSM
            {
                ActionTable = actions.ToArray(),
                SoundClipTable = soundclips.ToArray(),
                Paths = paths,
                EntityTable = entities,
                Constants = datas.ToArray(),
                StackMachines = machines,
                ByteCode = code.ToArray()
            };
        }

        public static byte[] CompileADEFData(byte[] fsmData)
        {
            using (var memstream = new MemoryStream())
            {
                using(var bw = new BinaryWriter(memstream))
                {
                    bw.WriteCString("AREV", 4);
                    bw.Write(4 + 8);
                    bw.Write(1);

                    bw.WriteCString("FSM", 4);
                    bw.Write(fsmData.Length + 8);
                    bw.Write(fsmData);

                    bw.WriteCString("EXIT", 4);
                    bw.Write(8);
                    bw.Flush();
                }
                return memstream.ToArray();
            }
        }

        public static byte[] CompileFSMData(FSM fsm)
        {
            using (var memStream = new MemoryStream())
            {
                using (var bw = new BinaryWriter(memStream))
                {
                    bw.Write(fsm.ActionTable.Length);
                    foreach (var action in fsm.ActionTable)
                    {
                        bw.WriteCString(action, 40);
                    }

                    bw.Write(fsm.EntityTable.Count);
                    foreach (var entity in fsm.EntityTable)
                    {
                        bw.WriteCString(entity.Label, 40);
                        bw.WriteCString(entity.Value, 8);
                    }

                    bw.Write(fsm.SoundClipTable.Length);
                    foreach (var soundclip in fsm.SoundClipTable)
                    {
                        bw.WriteCString(soundclip, 40);
                    }
                    
                    bw.Write(fsm.Paths.Count);
                    foreach (var path in fsm.Paths)
                    {
                        bw.WriteCString(path.Name, 40);
                        bw.Write(path.Nodes.Length);
                        foreach (var node in path.Nodes)
                        {
                            bw.Write(node.x);
                            bw.Write(node.y);
                            bw.Write(node.z);
                        }
                    }

                    bw.Write(fsm.StackMachines.Count);
                    foreach (var machine in fsm.StackMachines)
                    {
                        var next = bw.BaseStream.Position + 168;

                        bw.Write(machine.StartAddress);

                        bw.Write(machine.InitialArguments.Length);
                        foreach (var arg in machine.InitialArguments)
                        {
                            bw.Write(arg);
                        }

                        bw.BaseStream.Position = next;
                    }

                    bw.Write(fsm.Constants.Length);
                    foreach (var constant in fsm.Constants)
                    {
                        bw.Write(constant);
                    }

                    bw.Write(fsm.ByteCode.Length);
                    foreach (var byteCode in fsm.ByteCode)
                    {
                        bw.Write((uint)byteCode.OpCode);
                        bw.Write(byteCode.Value);
                    }

                    bw.Flush();
                }
                
                return memStream.ToArray();
            }
        }

        public static void WriteMission(string filename, FSM fsm, string outputFilename)
        {
            var tagList = new List<TagChunk>();
            using (var msn = new Bwd2Reader(new FileStream(filename, FileMode.Open)))
            {
                var current = msn.Current;
                while (current != null)
                {
                    var data = new byte[current.DataLength];
                    msn.BaseStream.Position = current.DataPosition;
                    msn.Read(data, 0, (int)current.DataLength);
                    var tag = new TagChunk
                    {
                        TagName = current.Name,
                        Data = data
                    };
                    tagList.Add(tag);
                    
                    current = current.Next;
                }                
            }

            var adefTag = tagList.SingleOrDefault(x => x.TagName == "ADEF");
            if (adefTag == null)
                throw new System.Exception("No ADEF tag in mission file");

            var fsmData = CompileFSMData(fsm);
            adefTag.Data = CompileADEFData(fsmData);

            using (var bw = new BinaryWriter(new FileStream(outputFilename, FileMode.Create)))
            {
                foreach (var tag in tagList)
                {
                    bw.WriteCString(tag.TagName, 4);
                    bw.Write(tag.Data.Length + 8);
                    bw.Write(tag.Data);
                }
            }
        }

        public static FSM ReadMission(string filename)
        {
            using (var msn = new Bwd2Reader(new FileStream(filename, FileMode.Open)))
            {
                msn.FindNext("ADEF");
                using (var adef = new Bwd2Reader(msn))
                {
                    adef.FindNext("FSM");
                    var fsm = new FSM();

                    fsm.ActionTable = new string[adef.ReadUInt32()];
                    for (int i = 0; i < fsm.ActionTable.Length; i++)
                    {
                        fsm.ActionTable[i] = adef.ReadCString(40);
                    }

                    var numEntities = adef.ReadUInt32();
                    fsm.EntityTable = new List<FSMEntity>();
                    var uniqueValues = new Dictionary<string, int>();

                    for (int i = 0; i < numEntities; i++)
                    {
                        var label = adef.ReadCString(40);
                        var value = adef.ReadCString(8);

                        var valueIndex = 0;
                        if (uniqueValues.ContainsKey(value))
                        {
                            valueIndex = uniqueValues[value] + 1;
                        }
                        uniqueValues[value] = valueIndex;

                        var uniqueValue = value + "_" + valueIndex;

                        fsm.EntityTable.Add(new FSMEntity
                        {
                            Label = label,
                            Value = value,
                            UniqueValue = uniqueValue
                        });
                    }

                    fsm.SoundClipTable = new string[adef.ReadUInt32()];
                    for (int i = 0; i < fsm.SoundClipTable.Length; i++)
                    {
                        fsm.SoundClipTable[i] = adef.ReadCString(40);
                    }

                    var numPaths = adef.ReadUInt32();
                    fsm.Paths = new List<FSMPath>();
                    for (int i = 0; i < numPaths; i++)
                    {
                        var name = adef.ReadCString(40);
                        var nodes = new I76Vector3[adef.ReadUInt32()];
                        for (int p = 0; p < nodes.Length; p++)
                        {
                            nodes[p] = new I76Vector3(adef.ReadSingle(), adef.ReadSingle(), adef.ReadSingle());
                        }
                        fsm.Paths.Add(new FSMPath
                        {
                            Name = name,
                            Nodes = nodes
                        });
                    }

                    fsm.StackMachines = new List<StackMachine>();
                    var numMachines = adef.ReadUInt32();
                    for (int i = 0; i < numMachines; i++)
                    {
                        var next = adef.BaseStream.Position + 168;

                        var machine = new StackMachine();
                        machine.StartAddress = adef.ReadUInt32();
                        machine.InitialArguments = new int[adef.ReadUInt32()];

                        for (int j = 0; j < machine.InitialArguments.Length; j++)
                        {
                            machine.InitialArguments[j] = adef.ReadInt32();
                        }
                        adef.BaseStream.Position = next;

                        fsm.StackMachines.Add(machine);
                    }

                    fsm.Constants = new int[adef.ReadUInt32()];
                    for (int i = 0; i < fsm.Constants.Length; i++)
                    {
                        fsm.Constants[i] = adef.ReadInt32();
                    }

                    fsm.ByteCode = new ByteCode[adef.ReadUInt32()];
                    for (int i = 0; i < fsm.ByteCode.Length; i++)
                    {
                        var byteCode = fsm.ByteCode[i] = new ByteCode();
                        byteCode.OpCode = (OpCode)adef.ReadUInt32();
                        byteCode.Value = adef.ReadInt32();
                    }
                    return fsm;
                }
            }
        }
    }
}
