using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.System
{
    class FSMRunner : MonoBehaviour
    {
        public float FPSDelay = 0.5f; // Run twice every second
        private float _nextUpdate = 0;

        public FSM FSM;
        private int _resultReg = 0;
        private Stack<int> _argumentStack = new Stack<int>();

        private void Start()
        {
        }

        public void Update()
        {
            if (FSM == null)
                return;

            if (Time.unscaledTime >= _nextUpdate)
            {
                RunMachines();
                _nextUpdate = Time.unscaledTime + FPSDelay;
            }
        }

        private void RunMachines()
        {
            var currentMachineIndex = 0;
            //while (currentMachineIndex < FSM.StackMachines.Count)
            //{
            var machine = FSM.StackMachines[currentMachineIndex];
            if (Step(machine) == StepResult.DoNextMachine)
                currentMachineIndex++;
            //}            
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
                    var sVal = machine.Stack[byteCode.Value];

                    _argumentStack.Push(sVal);
                    break;
                case OpCode.ARGPUSH_B:
                    var idx = machine.Constants.Length - byteCode.Value;
                    var bVal = machine.Constants[idx];

                    _argumentStack.Push(bVal);
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
                    if (_resultReg == 0)
                        machine.IP = (uint)byteCode.Value;
                    break;
                case OpCode.JMP_I:
                    machine.IP = (uint)byteCode.Value;
                    return StepResult.DoNextMachine;
                case OpCode.RST:
                    machine.Reset();
                    break;
                case OpCode.ACTION:
                    _resultReg = 0; // Just for now, all return 0
                    break;
                case OpCode.NEG:
                    _resultReg = -_resultReg; // Verify
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
