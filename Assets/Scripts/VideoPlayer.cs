using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public struct SmkAudioData
    {
        public byte TrackMask;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] AudioChannels;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public int[] AudioRate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] BitDepth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public int[] AudioDataSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public IntPtr[] AudioData;
    }

    public class VideoPlayer : IDisposable
    {
        [DllImport("libsmacker")]
        private static extern IntPtr OpenFile(string filePath, ref int width, ref int height, ref int frameCount, ref float microSecondsPerFrame);
        [DllImport("libsmacker")]
        private static extern IntPtr GetAudioData(IntPtr smkFileHandle);
        [DllImport("libsmacker")]
        private static extern void DisposeFile(IntPtr smkFileHandle);
        [DllImport("libsmacker")]
        private static extern void DisposeAudioData(IntPtr audioDataHandle);
        [DllImport("libsmacker")]
        private static extern void GetFrameData(IntPtr smkFileHandle, [In, Out] byte[] buffer, uint bufferSize);
        [DllImport("libsmacker")]
        private static extern bool AdvanceFrame(IntPtr smkFileHandle);

        public RawImage OutputImage;

        private IntPtr _videoFileHandle;
        private byte[] _frameBuffer;
        private Color32[] _frameColors;
        private Texture2D _outputTexture;
        private int _width;
        private int _height;
        private int _frameCount;
        private float _secondsPerFrame;
        private float _videoTimer;
        private readonly string _fileName;
        private GameObject _audioObj;
        private List<AudioSource> _audioTracks;

        public VideoPlayer(string videoName)
        {
            if (string.IsNullOrEmpty(videoName))
            {
                return;
            }

            _fileName = videoName;
            string videoFolder = Path.Combine(Game.Instance.GamePath, "CUTSCENE");
            if (!Directory.Exists(videoFolder))
            {
                videoFolder = Path.Combine(Game.Instance.GamePath, "smk"); // GoG version
            }

            string filePath = Path.Combine(videoFolder, videoName);

            if (!File.Exists(filePath))
            {
                Debug.LogError("Video file '" + videoName + "' does not exist.");
                return;
            }

            LoadVideo(filePath);
        }

        private void LoadVideo(string videoFilePath)
        {
            _videoFileHandle = OpenFile(videoFilePath, ref _width, ref _height, ref _frameCount, ref _secondsPerFrame);
            _secondsPerFrame *= 1e-6f;

            if (_videoFileHandle == IntPtr.Zero || _height == 0 || _width == 0 || _frameCount == 0)
            {
                if (_videoFileHandle != IntPtr.Zero)
                {
                    DisposeFile(_videoFileHandle);
                    _videoFileHandle = IntPtr.Zero;
                }

                Debug.LogError("Error opening video file '" + _fileName + "'.");
                return;
            }

            _frameBuffer = new byte[_width * _height * 3];
            _frameColors = new Color32[_width * _height];
            _outputTexture = new Texture2D(_width, _height, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Debug.Log("Playing video '" + _fileName + "' - " + _width + "x" + _height + ", duration: " + (_frameCount * _secondsPerFrame) + " seconds.");

            LoadAudio();
        }

        private void LoadAudio()
        {
            IntPtr audioDataHandle = GetAudioData(_videoFileHandle);
            if (audioDataHandle == IntPtr.Zero)
            {
                return;
            }

            _audioTracks = new List<AudioSource>();
            SmkAudioData audioData = (SmkAudioData)Marshal.PtrToStructure(audioDataHandle, typeof(SmkAudioData));
            for (int i = 0; i < 7; ++i)
            {
                // SMK files can have up to 7 audio tracks. As far as I know I76 only uses one, but we'll check anyway.
                if ((audioData.TrackMask & 1 << i) != 1)
                {
                    continue;
                }

                int trackSize = audioData.AudioDataSize[i];
                float[] audioSamples = new float[trackSize];

                byte[] audioBytes = new byte[audioData.AudioDataSize[i]];
                Marshal.Copy(audioData.AudioData[i], audioBytes, 0, audioData.AudioDataSize[i]);
            
                // All the SMK files use 8-bit audio so we can simply divide by 255 to get the float value.
                for (int j = 0; j < trackSize; ++j)
                {
                    audioSamples[j] = audioBytes[j] / 255f;
                }

                _audioObj = new GameObject("Video Audio");
                AudioSource audioSource = _audioObj.AddComponent<AudioSource>();
                AudioClip audioClip = AudioClip.Create("Track " + (i + 1), trackSize, audioData.AudioChannels[i], audioData.AudioRate[i], false);
                audioClip.SetData(audioSamples, 0);
                audioSource.clip = audioClip;
                audioSource.Play();
                _audioTracks.Add(audioSource);
            }

            DisposeAudioData(audioDataHandle);
        }

        public IEnumerator PlayVideo()
        {
            if (OutputImage == null)
            {
                Debug.LogError("Output target texture for video not set.");
                yield break;
            }

            float inverseAspectRatio = (float)_height / _width;
            OutputImage.rectTransform.sizeDelta = new Vector2(inverseAspectRatio * Screen.width * -0.5f, 0f);
            OutputImage.texture = _outputTexture;

            int frameSize = _width * _height * 3;
            while (_videoFileHandle != IntPtr.Zero)
            {
                _videoTimer += Time.deltaTime;
                while (_videoTimer > 0f)
                {
                    GetFrameData(_videoFileHandle, _frameBuffer, (uint)_frameBuffer.Length);

                    int colorIndex = 0;
                    for (int i = 0; i < frameSize; i += 3)
                    {
                        _frameColors[colorIndex].r = _frameBuffer[i];
                        _frameColors[colorIndex].g = _frameBuffer[i + 1];
                        _frameColors[colorIndex].b = _frameBuffer[i + 2];
                        ++colorIndex;
                    }

                    _outputTexture.SetPixels32(_frameColors);
                    _outputTexture.Apply(false, false);

                    if (!AdvanceFrame(_videoFileHandle))
                    {
                        OutputImage.texture = Texture2D.blackTexture;
                        Cleanup();
                    }

                    _videoTimer -= _secondsPerFrame;
                }

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
                {
                    Cleanup();
                    yield return null;
                    yield break;
                }

                yield return null;
            }
        }

        public void Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (_audioObj != null)
            {
                UnityEngine.Object.Destroy(_audioObj);
                _audioObj = null;
            }

            if (_outputTexture != null)
            {
                UnityEngine.Object.Destroy(_outputTexture);
                _outputTexture = null;
            }

            if (_videoFileHandle != IntPtr.Zero)
            {
                DisposeFile(_videoFileHandle);
                _videoFileHandle = IntPtr.Zero;
            }

            if (_audioTracks != null)
            {
                for (int i = 0; i < _audioTracks.Count; ++i)
                {
                    UnityEngine.Object.Destroy(_audioTracks[i].clip);
                    UnityEngine.Object.Destroy(_audioTracks[i]);
                }
                _audioTracks = null;
            }

            _frameBuffer = null;
        }
    }
}