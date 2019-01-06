using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities;
using Assets.System;
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
            var args = machine.ArgumentQueue;
            switch (actionName)
            {
                case "null":
                    // Do nothing?
                    break;
                case "true":
                    return 1;
                case "inc":
                    {
                        var arg = args.Dequeue();
                        ++arg.Value;
                    }
                    break;
                case "dec":
                    {
                        var arg = args.Dequeue();
                        --arg.Value;
                    }
                    break;
                case "set":
                    {
                        var arg = args.Dequeue();
                        var val = args.Dequeue();
                        arg.Value = val.Value;
                    }
                    break;
                case "isGreater":
                {
                    var val = args.Dequeue();
                    var number = args.Dequeue();

                    bool greater = val.Value > number.Value;
                    return greater ? 1 : 0;
                }
                case "isLesser":
                {
                    var val = args.Dequeue();
                    var number = args.Dequeue();

                    bool lesser = val.Value < number.Value;
                    return lesser ? 1 : 0;
                }
                case "isEqual":
                {
                    var val = args.Dequeue();
                    var number = args.Dequeue();

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
                    var timerNo = args.Dequeue();
                    var seconds = args.Dequeue();

                    var secondsElapsed = Time.unscaledTime - fsmRunner.Timers[timerNo.Value];
                    return secondsElapsed >= seconds.Value ? 1 : 0;
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
                        var origoEntity = fsmRunner.FSM.EntityTable[whichEntity.Value];
                        var entity = origoEntity.Object;

                        var relativePos = new Vector3(args.Dequeue().Value, args.Dequeue().Value, args.Dequeue().Value) / 100.0f;

                        var yaw = args.Dequeue().Value;
                        var roll = args.Dequeue().Value;
                        var pitch = args.Dequeue().Value;

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
                        var pathIndex = args.Dequeue().Value;
                        var height = args.Dequeue().Value;
                        var watchTarget = args.Dequeue().Value;
                        
                        var path = fsmRunner.FSM.Paths[pathIndex];

                        Vector3 nodePos = path.GetWorldPosition(0);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        camera.transform.position = nodePos;

                        var entity = fsmRunner.FSM.EntityTable[watchTarget].Object;
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
                        var entityIndex = args.Dequeue().Value;
                        var pathIndex = args.Dequeue().Value;
                        var targetSpeed = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
                        var path = fsmRunner.FSM.Paths[pathIndex];

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
                        var pathIndex = args.Dequeue().Value;
                        var entityIndex = args.Dequeue().Value;
                        var distance = args.Dequeue().Value;

                        var path = fsmRunner.FSM.Paths[pathIndex];
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
                            
                        Car car = entity.Object.GetComponent<Car>();
                        if (car != null)
                        {
                            // NOTE: Despite the action name 'IsWithinNav', it looks like it should return 0 when true, on comparing results with the original game.
                            bool within = car.IsWithinNav(path, distance);
                            return within ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithinSqNav":
                    {
                        var pathIndex = args.Dequeue().Value;
                        var entityIndex = args.Dequeue().Value;
                        var distance = args.Dequeue().Value;

                        var path = fsmRunner.FSM.Paths[pathIndex];
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

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
                        var entityIndex = args.Dequeue().Value;
                        var targetIndex = args.Dequeue().Value;
                        var unk1 = args.Dequeue().Value;
                        var unk2 = args.Dequeue().Value;
                        var xOffset = args.Dequeue().Value;
                        var targetSpeed = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
                        var targetEntity = fsmRunner.FSM.EntityTable[targetIndex];

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
                        var entityIndex = args.Dequeue().Value;
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

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
                        var entityIndex = args.Dequeue().Value;
                        var pathIndex = args.Dequeue().Value;
                        var targetSpeed = args.Dequeue().Value;
                        var height = args.Dequeue().Value;
                        
                        var path = fsmRunner.FSM.Paths[pathIndex];
                       
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

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
                        var entityIndex = args.Dequeue().Value;
                        var origoEntity = fsmRunner.FSM.EntityTable[entityIndex];
                        var entity = origoEntity.Object;

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
                        var entityIndex = args.Dequeue().Value;
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

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
                        var entityIndex = args.Dequeue().Value;
                        var avoidIndex = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
                        var avoidEntity = fsmRunner.FSM.EntityTable[avoidIndex];

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
                        var entityIndex = args.Dequeue().Value;
                        var maxAttackers = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];

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
                        var entityIndex = args.Dequeue().Value;
                        var skill1 = args.Dequeue().Value;
                        var skill2 = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
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
                        var entityIndex = args.Dequeue().Value;
                        var aggressionValue = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
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
                        var entityIndex = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
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
                        var entityIndex = args.Dequeue().Value;

                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
                        if (entity.WorldEntity != null)
                        {
                            return entity.WorldEntity.Alive ? 0 : 1;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithin":
                    { 
                        var entityIndex = args.Dequeue().Value;
                        var targetIndex = args.Dequeue().Value;
                        var distance = args.Dequeue().Value;
                        
                        var entity = fsmRunner.FSM.EntityTable[entityIndex];
                        var target = fsmRunner.FSM.EntityTable[targetIndex];

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
                    var timerIndex = args.Dequeue().Value;
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
