using Assets.Scripts.Camera;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class FSMActionDelegator
    {
        private void LogUnhandledEntity(string actionName, int entityIndex, FSMEntity entity, StackMachine machine)
        {
            Debug.LogWarning("FSM action '" + actionName + "' not implemented for entity " + entityIndex + " (" + entity.Value + ") @ " + (machine.IP - 1));
        }

        public int DoAction(string actionName, StackMachine machine, FSMRunner fsmRunner)
        {
            global::System.Collections.Generic.Queue<IntRef> args = machine.ArgumentQueue;
            switch (actionName)
            {
                case "null":
                    // Do nothing?
                    break;
                case "true":
                    return 1;
                case "inc":
                    {
                        IntRef arg = args.Dequeue();
                        ++arg.Value;
                    }
                    break;
                case "dec":
                    {
                        IntRef arg = args.Dequeue();
                        --arg.Value;
                    }
                    break;
                case "set":
                    {
                        IntRef arg = args.Dequeue();
                        IntRef val = args.Dequeue();
                        arg.Value = val.Value;
                    }
                    break;
                case "isGreater":
                {
                        IntRef val = args.Dequeue();
                        IntRef number = args.Dequeue();

                    bool greater = val.Value > number.Value;
                    return greater ? 1 : 0;
                }
                case "isLesser":
                {
                        IntRef val = args.Dequeue();
                        IntRef number = args.Dequeue();

                    bool lesser = val.Value < number.Value;
                    return lesser ? 1 : 0;
                }
                case "isEqual":
                {
                        IntRef val = args.Dequeue();
                        IntRef number = args.Dequeue();

                    bool equal = val.Value == number.Value;
                    return equal ? 1 : 0;
                }
                case "pushCam":
                    CameraManager.Instance.PushCamera();
                    break;
                case "popCam":
                    CameraManager.Instance.PopCamera();
                    break;
                case "timeGreater":
                    IntRef timerNo = args.Dequeue();
                    IntRef seconds = args.Dequeue();

                    float secondsElapsed = Time.unscaledTime - fsmRunner.Timers[timerNo.Value];
                    return secondsElapsed >= seconds.Value ? 1 : 0;
                case "isKeypress":
                    return Input.GetKeyDown(KeyCode.Space) ? 1 : 0;
                case "camObjDir":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        UnityEngine.Camera camera = CameraManager.Instance.ActiveCamera;
                        IntRef whichEntity = args.Dequeue();
                        FSMEntity origoEntity = fsmRunner.FSM.EntityTable[whichEntity.Value];
                        GameObject entity = origoEntity.Object;

                        Vector3 relativePos = new Vector3(args.Dequeue().Value, args.Dequeue().Value, args.Dequeue().Value) / 100.0f;

                        int yaw = args.Dequeue().Value;
                        int roll = args.Dequeue().Value;
                        int pitch = args.Dequeue().Value;

                        Vector3 rotation = new Vector3(yaw, pitch, roll) / 100.0f;

                        Vector3 newPos = entity.transform.position + (entity.transform.rotation * relativePos);

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

                        UnityEngine.Camera camera = CameraManager.Instance.ActiveCamera;
                        int pathIndex = args.Dequeue().Value;
                        int height = args.Dequeue().Value;
                        int watchTarget = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        Vector3 nodePos = path.GetWorldPosition(0);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        camera.transform.position = nodePos;

                        GameObject entity = fsmRunner.FSM.EntityTable[watchTarget].Object;
                        camera.transform.LookAt(entity.transform, Vector3.up);
                    }
                    break;
                case "camObjObj":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        int objectIndex = args.Dequeue().Value;
                        float xPos = args.Dequeue().Value;
                        float yPos = args.Dequeue().Value;
                        float zPos = args.Dequeue().Value;
                        int watchTarget = args.Dequeue().Value;
                        
                        FSMEntity anchorEntity = fsmRunner.FSM.EntityTable[objectIndex];
                        FSMEntity targetEntity = fsmRunner.FSM.EntityTable[watchTarget];

                        UnityEngine.Camera camera = CameraManager.Instance.ActiveCamera;
                        camera.transform.SetParent(anchorEntity.Object.transform);
                        camera.transform.localPosition = new Vector3(xPos * 0.01f, zPos * 0.01f, yPos * 0.01f);
                        camera.transform.LookAt(targetEntity.Object.transform, Vector3.up);
                    }
                    break;
                case "goto":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int pathIndex = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            car.SetTargetPath(path, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithinNav":
                    {
                        int pathIndex = args.Dequeue().Value;
                        int entityIndex = args.Dequeue().Value;
                        int distance = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                            
                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            bool within = car.IsWithinNav(path, distance);
                            return within ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithinSqNav":
                    {
                        int pathIndex = args.Dequeue().Value;
                        int entityIndex = args.Dequeue().Value;
                        int distance = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            bool within = car.IsWithinNav(path, (int)Mathf.Sqrt(distance));
                            return within ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "follow":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int targetIndex = args.Dequeue().Value;
                        int unk1 = args.Dequeue().Value;
                        int unk2 = args.Dequeue().Value;
                        int xOffset = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMEntity targetEntity = fsmRunner.FSM.EntityTable[targetIndex];

                        Car car = entity.Object.GetComponent<Car>();
                        Car targetCar = targetEntity.Object.GetComponent<Car>();
                        if (car != null && targetCar != null)
                        {
                            car.SetFollowTarget(targetCar, xOffset, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isAtFollow":
                    {
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            bool atFollow = car.AtFollowTarget();
                            return atFollow ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "teleport":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int pathIndex = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;
                        int height = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Vector3 nodePos = path.GetWorldPosition(0);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        entity.Object.transform.position = nodePos;

                        Car car = entity.Object.GetComponent<Car>();
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
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity origoEntity = fsmRunner.FSM.EntityTable[entityIndex];
                        GameObject entity = origoEntity.Object;

                        Car car = entity.GetComponent<Car>();
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
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            car.Sit();
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setAvoid":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int avoidIndex = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMEntity avoidEntity = fsmRunner.FSM.EntityTable[avoidIndex];

                        Car car = entity.Object.GetComponent<Car>();
                        WorldEntity target = avoidEntity.WorldEntity;
                        if (car != null && target != null)
                        {
                            // TODO: Figure out 'avoid' logic - don't path near object?
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setMaxAttackers":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int maxAttackers = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        if (entity.WorldEntity != null)
                        {
                            entity.WorldEntity.MaxAttackers = maxAttackers;
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setSkill":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int skill1 = args.Dequeue().Value;
                        int skill2 = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            car.Skill1 = skill1;
                            car.Skill2 = skill2;
                            return 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setAgg":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int aggressionValue = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            car.Aggressiveness = aggressionValue;
                            return 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isAttacked":
                    {
                        int entityIndex = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            return car.Attacked ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isDead":
                    {
                        int entityIndex = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        if (entity.WorldEntity != null)
                        {
                            return entity.WorldEntity.Alive ? 0 : 1;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithin":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int targetIndex = args.Dequeue().Value;
                        int distance = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMEntity target = fsmRunner.FSM.EntityTable[targetIndex];

                        bool within = Vector3.Distance(entity.Object.transform.position, target.Object.transform.position) < distance;
                        return within ? 1 : 0;
                    }
                case "cbFromPrior":
                    {
                        int soundId = args.Dequeue().Value;
                        int owner = args.Dequeue().Value;
                        int queueFlag = args.Dequeue().Value;
                        QueueRadio(fsmRunner, soundId, queueFlag, owner);
                    }
                    break;
                case "cbPrior":
                    {
                        int soundId = args.Dequeue().Value;
                        int queueFlag = args.Dequeue().Value;
                        QueueRadio(fsmRunner, soundId, queueFlag, -1);
                    }
                    break;
                case "rand":
                    {
                        IntRef arg = args.Dequeue();
                        IntRef val = args.Dequeue();
                        arg.Value = Random.Range(0, val.Value);
                    }
                    break;
                case "stopCB":
                    RadioManager.Instance.Stop();
                    break;
                case "isCBEmpty":
                    {
                        return RadioManager.Instance.IsQueueEmpty() ? 1 : 0;
                    }
                case "startTimer":
                    int timerIndex = args.Dequeue().Value;
                    fsmRunner.Timers[timerIndex] = Time.unscaledTime;
                    break;
                default:
                    Debug.LogWarning("FSM action not implemented: " + actionName + " @ " + (machine.IP-1));
                    break;
            }

            return 0;
        }

        private void QueueRadio(FSMRunner fsmRunner, int soundId, int queueFlag, int owner)
        {
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
            RadioManager.Instance.QueueRadioMessage(soundName, endOfQueue, owner);
        }
    }
}
