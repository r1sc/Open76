using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.System
{
    class FSMRunner : MonoBehaviour
    {
        public float[] Timers { get; set; }
        public float FPSDelay = 0.5f; // Run twice every second
        private float _nextUpdate = 0;

        public FSM FSM;

        private void Start()
        {
            Timers = new float[10];
            StartCoroutine(RunMachines());
        }

        public void Update()
        {

        }

        private IEnumerator RunMachines()
        {
            while (true)
            {
                if (FSM == null)
                    yield return null;

                var currentMachineIndex = 0;
                while (currentMachineIndex < FSM.StackMachines.Length)
                {
                    var machine = FSM.StackMachines[currentMachineIndex];
                    if (machine.Halted || (Step(machine) == StepResult.DoNextMachine))
                    {
                        currentMachineIndex++;
                        yield return null;
                    }
                }
                yield return null;
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

                    machine.ArgumentQueue.Enqueue(bVal);
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
                    return StepResult.DoNextMachine;
                case OpCode.ACTION:
                    var actionName = FSM.ActionTable[byteCode.Value];

                    machine.ResultReg = FSMActionDelegator.DoAction(actionName, machine, this);
                    if(machine.ArgumentQueue.Count != 0)
                    {
                        int hej = 2;
                    }
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
