using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class FSM
    {
        public string[] ActionTable { get; set; }
        public Dictionary<string, string> EntityTable { get; set; }
        public string[] SoundClipTable { get; set; }
        public Dictionary<string, Vector3[]> Paths { get; set; }

        public List<StackMachine> StackMachines { get; set; }
        public int[] Variables { get; set; }
        public ByteCode[] ByteCode { get; set; }
    }
    
    public enum OpCode : uint
    {
        Unknown = 0,
        PUSH = 1,
        Unused1 = 2,
        Unused2 = 3,
        COPY_S = 4,
        COPY_B = 5,
        STACK_MOD = 6, // Adjust by arg
        POP = 7, // Set stack pointer to SP-arg
        JMP = 8,
        JZ = 9,
        JMP_I = 10,
        Unused3 = 11,
        RST = 12,
        ACTION = 13,
        NEG = 14,   // No arg
        Unused4 = 15
    }

    public class ByteCode
    {
        public OpCode OpCode { get; set; }
        public int Value { get; set; }

        public override string ToString()
        {
            return Enum.GetName(typeof(OpCode), OpCode) + ": " + Value;
        }
    }

    public class StackMachine
    {
        public uint StartAddress { get; set; }
        public int[] InitialArguments { get; set; }
    }
}
