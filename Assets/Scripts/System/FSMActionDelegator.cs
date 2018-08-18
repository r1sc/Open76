using Assets.Scripts.Camera;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class FSMActionDelegator
    {
        private Transform _worldTransform;

        public FSMActionDelegator()
        {
            _worldTransform = GameObject.Find("World").transform;
        }

        private void LogUnhandledEntity(string actionName, int entityIndex, FSMEntity entity, StackMachine machine)
        {
            Debug.LogWarning("FSM action '" + actionName + "' not implemented for entity " + entityIndex + " (" + entity.Value + ") @ " + (machine.IP - 1));
        }

        public int DoAction(string actionName, StackMachine machine, FSMRunner fsmRunner)
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
                    CameraManager.Instance.PushCamera();
                    break;
                case "popCam":
                    CameraManager.Instance.PopCamera();
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
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        var camera = CameraManager.Instance.ActiveCamera;
                        var whichEntity = args.Dequeue();
                        var origoEntity = fsmRunner.FSM.EntityTable[whichEntity];
                        var entity = origoEntity.Object;

                        var relativePos = new Vector3(args.Dequeue(), args.Dequeue(), args.Dequeue()) / 100.0f;

                        var yaw = args.Dequeue();
                        var roll = args.Dequeue();
                        var pitch = args.Dequeue();

                        var rotation = new Vector3(yaw, pitch, roll) / 100.0f;

                        var newPos = entity.transform.position + (entity.transform.rotation * relativePos);

                        if (newPos.y < entity.transform.position.y + 1)
                            newPos.y = entity.transform.position.y + 1;

                        camera.transform.position = newPos;
                        camera.transform.rotation = entity.transform.rotation * Quaternion.Euler(rotation);
                    }
                    break;
                case "camPosObj":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        var camera = CameraManager.Instance.ActiveCamera;
                        var pathIndex = args.Dequeue();
                        var height = args.Dequeue();
                        var watchTarget = args.Dequeue();
                        
                        var path = fsmRunner.FSM.Paths[pathIndex];

                        Vector3 nodePos = path.GetWorldPosition(0);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        camera.transform.position = nodePos;

                        var entity = fsmRunner.FSM.EntityTable[watchTarget].Object;
                        camera.transform.LookAt(entity.transform, Vector3.up);
                    }
                    break;
                case "goto":
                    {
                        var entityIndex = args.Dequeue();
                        var pathIndex = args.Dequeue();
                        var targetSpeed = args.Dequeue();

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
                        var path = fsmRunner.FSM.Paths[pathIndex];

                        CarAI car = entity.Object.GetComponent<CarAI>();
                        if (car != null)
                        {
                            car.SetTargetPath(path, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "teleport":
                    {
                        var entityIndex = args.Dequeue();
                        var pathIndex = args.Dequeue();
                        var targetSpeed = args.Dequeue();
                        var height = args.Dequeue();
                        
                        var path = fsmRunner.FSM.Paths[pathIndex];
                       
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Vector3 nodePos = path.GetWorldPosition(0);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        entity.Object.transform.position = nodePos;

                        CarAI car = entity.Object.GetComponent<CarAI>();
                        if (car != null)
                        {
                            car.SetSpeed(targetSpeed);
                            car.SetTargetPath(path, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isArrived":
                    {
                        var entityIndex = args.Dequeue();
                        var origoEntity = fsmRunner.FSM.EntityTable[entityIndex];
                        var entity = origoEntity.Object;

                        CarAI car = entity.GetComponent<CarAI>();
                        if (car != null)
                        {
                            if (car.Arrived)
                            {
                                car.Arrived = false;
                                return 1;
                            }

                            return 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, origoEntity, machine);
                    }
                    break;
                case "sit":
                    {
                        var entityIndex = args.Dequeue();
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

                        CarAI car = entity.Object.GetComponent<CarAI>();
                        if (car != null)
                        {
                            car.Sit();
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isEqual":
                    {
                        var first = args.Dequeue();
                        var second = args.Dequeue();
                        return first == second ? 1 : 0;
                    }
                case "isAttacked":
                {
                    var entityIndex = args.Dequeue();
                    var entity = fsmRunner.FSM.EntityTable[entityIndex];

                    CarAI car = entity.Object.GetComponent<CarAI>();
                    if (car != null)
                    {
                        return car.Attacked ? 1 : 0;
                    }

                    LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isDead":
                    {
                        var entityIndex = args.Dequeue();
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

                        CarAI car = entity.Object.GetComponent<CarAI>();
                        if (car != null)
                        {
                            return car.Alive ? 0 : 1;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "cbPrior":
                    {
                        int soundId = args.Dequeue();
                        int queueFlag = args.Dequeue();
                        bool endOfQueue;
                        if (queueFlag == 1)
                        {
                            endOfQueue = false;
                        }
                        else if (queueFlag == 3)
                        {
                            endOfQueue = true;
                        }
                        else
                        {
                            endOfQueue = true;
                            Debug.Log("Unknown value in cbPrior: " + queueFlag);
                        }
                        
                        string soundName = fsmRunner.FSM.SoundClipTable[soundId];
                        RadioManager.Instance.QueueRadioMessage(soundName, endOfQueue);
                    }
                    break;
                case "rand":
                    {
                        int min = args.Dequeue();
                        int max = args.Dequeue();
                        return Random.Range(min, max);
                    }
                case "stopCB":
                    RadioManager.Instance.Stop();
                    break;
                case "isCBEmpty":
                    {
                        return RadioManager.Instance.IsQueueEmpty() ? 1 : 0;
                    }
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
