﻿using Assets.Scripts.I76Types;
using System;
using System.Collections.Generic;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class IntRef
    {
        public int Value { get; set; }

        public IntRef(int value)
        {
            Value = value;
        }
    }

    public class FSM
    {
        public string[] ActionTable { get; set; }
        public FSMEntity[] EntityTable { get; set; }

        public string[] SoundClipTable { get; set; }
        public FSMPath[] Paths { get; set; }

        public StackMachine[] StackMachines { get; set; }
        public IntRef[] Constants { get; set; }
        public ByteCode[] ByteCode { get; set; }
    }

    public class FSMEntity
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public int Id { get; set; }
        public GameObject Object { get; set; }
        public WorldEntity WorldEntity { get; set; }
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

    public struct ActionStackTrace
    {
        public string Action;
        public int[] Arguments;
        public int Result;
        public uint IP;
    }

    public class StackMachine
    {
        public uint StartAddress { get; set; }
        public int[] InitialArguments { get; set; }
        public IntRef[] Constants { get; set; }

        public uint IP { get; set; }
        public IndexableStack<int> Stack { get; set; }
        public int ResultReg { get; set; }
        public Queue<int> ArgumentQueue { get; set; }
        public bool Halted { get; set; }
        public Stack<ActionStackTrace> ActionStack { get; set; }
        public int LastAction { get; set; }

        public void Reset()
        {
            LastAction = -1;
            IP = StartAddress;
            Stack = new IndexableStack<int>();
            ArgumentQueue = new Queue<int>();
            ResultReg = 0;
            ActionStack = new Stack<ActionStackTrace>();
        }

        // TODO: Remove this and associated logic when done - only used for debugging FSM.
        public void PrintStack()
        {
            while (ActionStack.Count > 0)
            {
                ActionStackTrace stack = ActionStack.Pop();
                string logString = $"IP: {stack.IP} - Action: {stack.Action}";
                if (stack.Arguments != null)
                {
                    int argCount = stack.Arguments.Length;
                    for (int i = 0; i < argCount; ++i)
                    {
                        if (i == 0) logString += "(";
                        logString += stack.Arguments[i];
                        if (i == argCount - 1)
                        {
                            logString += ")";
                        }
                        else
                        {
                            logString += ", ";
                        }
                    }
                }

                Debug.LogFormat($"{logString} - Result: {stack.Result}");
            }
        }
    }

    public class FSMPath {
        public string Name { get; set; }
        public I76Vector3[] Nodes { get; set; }

        public Vector3 GetWorldPosition(int nodeIndex)
        {
            Transform world = GameObject.Find("World").transform;
            I76Vector3 nodePos = Nodes[nodeIndex];
            Vector3 worldPos = world.position;

            Vector3 output;
            output.x = worldPos.x + nodePos.x;
            output.y = worldPos.y + nodePos.y;
            output.z = worldPos.z + nodePos.z;
            return output;
        }
    }
}
