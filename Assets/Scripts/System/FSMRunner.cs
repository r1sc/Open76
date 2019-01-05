﻿using System;
using Assets.Scripts.I76Types;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class FSMRunner : MonoBehaviour
    {
        public static FSMRunner Instance { get; private set; }

        public float[] Timers { get; set; }

        public FSM FSM;
        private FSMActionDelegator _actionDelegator;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Timers = new float[10];
            _actionDelegator = new FSMActionDelegator();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            GameObject world = GameObject.Find("World");
            if (world == null) return;

            Vector3 worldPos = world.transform.position;

            FSMPath[] paths = FSM.Paths;
            for (int i = 0; i < paths.Length; ++i)
            {
                FSMPath path = paths[i];
                for (int j = 0; j < path.Nodes.Length - 1; ++j)
                {
                    Gizmos.DrawLine(worldPos + path.Nodes[j].ToVector3(), worldPos + path.Nodes[j + 1].ToVector3());
                }
            }
        }

        private void LateUpdate()
        {
            if (FSM == null)
            {
                return;
            }

            var currentMachineIndex = 0;
            while (currentMachineIndex < FSM.StackMachines.Length)
            {
                var machine = FSM.StackMachines[currentMachineIndex];
                if (machine.Halted || (Step(machine) == StepResult.DoNextMachine))
                {
                    currentMachineIndex++;
                }
            }
        }

        private StepResult Step(StackMachine machine)
        {
            var byteCode = FSM.ByteCode[machine.IP++];
            switch (byteCode.OpCode)
            {
                case OpCode.PUSH:
                    machine.Stack.Push(byteCode.Value);
                    break;
                case OpCode.ARGPUSH_S:
                    var sVal = machine.Stack[byteCode.Value - 1];

                    machine.ArgumentQueue.Enqueue(sVal);
                    break;
                case OpCode.ARGPUSH_B:
                    var idx = machine.Constants.Length + (byteCode.Value + 1);
                    var bVal = machine.Constants[idx];

                    machine.ArgumentQueue.Enqueue(bVal.Value);
                    break;
                case OpCode.ADJUST:
                    var addToSP = byteCode.Value;

                    if (addToSP < 1)
                        throw new NotImplementedException("What to do when adjusting 0 or negative values?");

                    for (int i = 0; i < addToSP; i++)
                    {
                        machine.Stack.Push(0);
                    }
                    break;
                case OpCode.DROP:
                    var subFromSP = byteCode.Value;

                    if (subFromSP < 0)
                        throw new NotImplementedException("Expecting positive values");

                    for (int i = 0; i < subFromSP; i++)
                    {
                        machine.Stack.Pop();
                    }
                    break;
                case OpCode.JMP:
                    machine.IP = (uint)byteCode.Value;
                    break;
                case OpCode.JZ:
                    if (machine.ResultReg == 0)
                        machine.IP = (uint)byteCode.Value;
                    break;
                case OpCode.JMP_I:
                    machine.IP = (uint)byteCode.Value;
                    return StepResult.DoNextMachine;
                case OpCode.RST:
                    machine.Halted = true;
                    machine.ActionStack.Clear();
                    return StepResult.DoNextMachine;
                case OpCode.ACTION:
                    var actionName = FSM.ActionTable[byteCode.Value];
                    ActionStackTrace trace = new ActionStackTrace
                    {
                        Action = actionName,
                        IP = machine.IP,
                        Arguments = machine.ArgumentQueue.ToArray()
                    };
                    machine.ResultReg = _actionDelegator.DoAction(actionName, machine, this);
                    trace.Result = machine.ResultReg;
                    if (machine.LastAction != byteCode.Value)
                    {
                        machine.ActionStack.Push(trace);
                    }
                    machine.LastAction = byteCode.Value;
                    machine.ArgumentQueue.Clear();
                    break;
                case OpCode.NEG:
                    machine.ResultReg = -machine.ResultReg; // Verify
                    break;
                default:
                    throw new NotImplementedException("Unimplemented bytecode " + byteCode.OpCode);
            }

            return StepResult.NotDone;
        }

        enum StepResult
        {
            NotDone,
            DoNextMachine
        }
    }
}
