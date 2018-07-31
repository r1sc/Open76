using Assets.Scripts.I76Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VR;

namespace Assets.Scripts.System
{
    public class FSM
    {
        public string[] ActionTable { get; set; }
        public FSMEntity[] EntityTable { get; set; }

        public string[] SoundClipTable { get; set; }
        public FSMPath[] Paths { get; set; }

        public StackMachine[] StackMachines { get; set; }
        public int[] Constants { get; set; }
        public ByteCode[] ByteCode { get; set; }
    }

    public class FSMEntity
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public int Id { get; set; }
        public GameObject Object { get; set; }
    }
    
    public enum OpCode : uint
    {
        Unknown = 0,
        PUSH = 1,
        Unused1 = 2,
        Unused2 = 3,
        ARGPUSH_S = 4,
        ARGPUSH_B = 5,
        ADJUST = 6, // Adjust by arg
        DROP = 7, // Set stack pointer to SP-arg
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
            return Enum.GetName(typeof(OpCode), OpCode).ToLower();
        }
    }

    public class StackMachine
    {
        public uint StartAddress { get; set; }
        public int[] InitialArguments { get; set; }
        public int[] Constants { get; set; }

        public uint IP { get; set; }
        public IndexableStack<int> Stack { get; set; }
        public int ResultReg { get; set; }
        public Queue<int> ArgumentQueue { get; set; }
        public bool Halted { get; set; }

        public void Reset()
        {
            IP = StartAddress;
            Stack = new IndexableStack<int>();
            ArgumentQueue = new Queue<int>();
            ResultReg = 0;
        }
    }

    public class FSMPath {
        public string Name { get; set; }
        public I76Vector3[] Nodes { get; set; }

        public Vector3 GetWorldPosition(int nodeIndex)
        {
            Transform world = GameObject.Find("World").transform;
            return world.position + Nodes[nodeIndex].ToVector3();
        }
    }
}
