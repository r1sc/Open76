using System.Collections.Generic;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class RadioManager : MonoBehaviour
    {
        private Queue<string> _radioMessageQueue;
        private AudioSource _radioSource;

        private void Awake()
        {
            _radioMessageQueue = new Queue<string>();
            _radioSource = gameObject.AddComponent<AudioSource>();
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

        public void QueueRadioMessage(string radioClip, bool endOfQueue)
        {
            if (endOfQueue)
            {
                _radioMessageQueue.Enqueue(radioClip);
            }
            else
            {
                Queue<string> tempQueue = new Queue<string>(_radioMessageQueue);
                _radioMessageQueue.Clear();
                _radioMessageQueue.Enqueue(radioClip);
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
                return;
            }

            DestroyAudioClip();
            if (_radioMessageQueue.Count > 0)
            {
                string fileName = _radioMessageQueue.Dequeue();

                AudioClip clip = VirtualFilesystem.Instance.GetAudioClip(fileName);
                if (clip != null)
                {
                    _radioSource.clip = clip;
                    _radioSource.Play();
                }
            }
        }
    }
}
