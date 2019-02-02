using System.Collections.Generic;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class RadioManager : MonoBehaviour
    {
        private Queue<RadioData> _radioMessageQueue;
        private AudioSource _radioSource;
        private Car _currentOwner;

        private struct RadioData
        {
            public string ClipName;
            public int Owner;
        }

        private void Awake()
        {
            _radioMessageQueue = new Queue<RadioData>();
            _radioSource = gameObject.AddComponent<AudioSource>();
            _radioSource.dopplerLevel = 0f;
            _radioSource.spatialize = false;
            _radioSource.priority = 0;
            _radioSource.volume = 0.4f;
        }

        private static RadioManager _instance;
        public static RadioManager Instance
        {
            get { return _instance ?? (_instance = GameObject.Find("World").AddComponent<RadioManager>()); }
        }

        public void Stop()
        {
            _radioSource.Stop();
            DestroyAudioClip();
        }

        private void DestroyAudioClip()
        {
            _currentOwner = null;
            if (_radioSource.clip != null)
            {
                Destroy(_radioSource.clip);
                _radioSource.clip = null;
            }
        }

        public bool IsQueueEmpty()
        {
            return _radioMessageQueue.Count == 0 && _radioSource.clip == null;
        }

        public void QueueRadioMessage(string radioClip, bool endOfQueue, int owner)
        {
            RadioData data = new RadioData();
            data.ClipName = radioClip;
            data.Owner = owner;

            if (endOfQueue)
            {
                _radioMessageQueue.Enqueue(data);
            }
            else
            {
                Queue<RadioData> tempQueue = new Queue<RadioData>(_radioMessageQueue);
                _radioMessageQueue.Clear();
                _radioMessageQueue.Enqueue(data);
                while (tempQueue.Count > 0)
                {
                    _radioMessageQueue.Enqueue(tempQueue.Dequeue());
                }
            }
        }

        private void OnDestroy()
        {
            DestroyAudioClip();
            _instance = null;
        }

        private void Update()
        {
            if (_radioSource.isPlaying)
            {
                if (_currentOwner == null || _currentOwner.Alive)
                {
                    return;
                }

                _radioSource.Stop();
                _currentOwner = null;
            }

            DestroyAudioClip();
            if (_radioMessageQueue.Count > 0)
            {
                RadioData radioData = _radioMessageQueue.Dequeue();

                if (radioData.Owner != -1)
                {
                    Car car = EntityManager.Instance.GetCar(radioData.Owner);
                    if (car == null || !car.Alive)
                    {
                        return;
                    }

                    _currentOwner = car;
                }

                AudioClip clip = VirtualFilesystem.Instance.GetAudioClip(radioData.ClipName);
                if (clip != null)
                {
                    _radioSource.clip = clip;
                    _radioSource.Play();
                }
            }
        }
    }
}
