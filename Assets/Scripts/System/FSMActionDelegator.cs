using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.System
{
    class FSMActionDelegator
    {
        
        public static int DoAction(string actionName, StackMachine machine, FSMRunner fsmRunner)
        {
            var args = machine.ArgumentQueue;
            switch (actionName)
            {
                case "null":
                    // Do nothing?
                    break;
                case "true":
                    return 1;
                case "pushCam":
                    // NO args
                    break;
                case "timeGreater":
                    var timerNo = args.Dequeue();
                    var seconds = args.Dequeue();

                    var secondsElapsed = Time.unscaledTime - fsmRunner.Timers[timerNo];
                    return secondsElapsed >= seconds ? 1 : 0;
                case "isKeypress":
                    return Input.GetKeyDown(KeyCode.Space) ? 1 : 0;
                case "camObjDir":
                    {
                        var whichEntity = args.Dequeue();
                        var origoEntity = fsmRunner.FSM.EntityTable[whichEntity];
                        var entity = GameObject.Find(origoEntity.UniqueValue);

                        var relativePos = new Vector3(args.Dequeue(), args.Dequeue(), args.Dequeue()) / 100.0f;

                        var yaw = args.Dequeue();
                        var roll = args.Dequeue();
                        var pitch = args.Dequeue();

                        var rotation = new Vector3(yaw, pitch, roll) / 100.0f;

                        var camera = GameObject.FindObjectOfType<CameraController>();
                        var newPos = entity.transform.position + (entity.transform.rotation * relativePos);

                        if (newPos.y < entity.transform.position.y + 1)
                            newPos.y = entity.transform.position.y + 1;

                        camera.transform.position = newPos;
                        camera.transform.rotation = entity.transform.rotation * Quaternion.Euler(rotation);
                    }
                    break;
                case "camPosObj":
                    {
                        var pathIndex = args.Dequeue();
                        var height = args.Dequeue();
                        var watchTarget = args.Dequeue();

                        var world = GameObject.Find("World");

                        var path = fsmRunner.FSM.Paths[pathIndex];
                        var camera = GameObject.FindObjectOfType<CameraController>();
                        camera.transform.position = world.transform.position + new Vector3(path.Nodes[0].x, path.Nodes[0].y + height, path.Nodes[1].z);
                        
                        var entity = GameObject.Find(fsmRunner.FSM.EntityTable[watchTarget].UniqueValue);
                        camera.transform.LookAt(entity.transform, Vector3.up);
                    }
                    break;
                case "startTimer":
                    var whichTimer = args.Dequeue();
                    fsmRunner.Timers[whichTimer] = Time.unscaledTime;
                    // TODO: Start the timer
                    break;
                default:
                    Debug.LogWarning("FSM action not implemented: " + actionName + " @ " + (machine.IP-1));
                    break;
            }

            return 0;
        }
    }
}
