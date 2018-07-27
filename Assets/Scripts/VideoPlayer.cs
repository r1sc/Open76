using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Assets.System;
using UnityEngine;
using UnityEngine.UI;

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

public class VideoPlayer : MonoBehaviour
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
    private static extern unsafe void GetFrameData(IntPtr smkFileHandle, Color32* buffer, uint bufferSize);
    [DllImport("libsmacker")]
    private static extern bool AdvanceFrame(IntPtr smkFileHandle);

    public string VideoFile;
    public RawImage OutputImage;

    private IntPtr _videoFileHandle;
    private Color32[] _frameBuffer;
    private Texture2D _outputTexture;
    private int _width;
    private int _height;
    private int _frameCount;
    private float _secondsPerFrame;
    private float _videoTimer;
    private List<AudioSource> _audioTracks;

    private void Awake()
    {
        if (string.IsNullOrEmpty(VideoFile))
        {
            return;
        }

        GameObject game = GameObject.Find("Game");
        CacheManager cacheManager = game.GetComponent<CacheManager>();
        string videoFolder = Path.Combine(cacheManager.GamePath, "CUTSCENE");
        string filePath = Path.Combine(videoFolder, VideoFile);

        if (!File.Exists(filePath))
        {
            Debug.LogError("Video file '" + VideoFile + "' does not exist.");
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

            Debug.LogError("Error opening video file '" + VideoFile + "'.");
            return;
        }

        _frameBuffer = new Color32[_width * _height];
        _outputTexture = new Texture2D(_width, _height, TextureFormat.RGB24, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        float inverseAspectRatio = (float)_height / _width;
        OutputImage.rectTransform.sizeDelta = new Vector2((int)(Screen.width * inverseAspectRatio), 0f);
        OutputImage.texture = _outputTexture;

        Debug.Log("Playing video '" + VideoFile + "' - " + _width + "x" + _height + ", duration: " + (_frameCount * _secondsPerFrame) + " seconds.");

        LoadAudio();

        StartCoroutine(PlayVideo());
    }

    private unsafe void LoadAudio()
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

            byte* nativeAudioData = (byte*)audioData.AudioData[i].ToPointer();
            
            // This probably won't work if the bit depth isn't 8 but most likely all the files use the same audio settings anyway.
            for (int j = 0; j < trackSize; ++j)
            {
                audioSamples[j] = nativeAudioData[j] / 255f;
            }

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            AudioClip audioClip = AudioClip.Create("Track " + (i + 1), trackSize, audioData.AudioChannels[i], audioData.AudioRate[i], false);
            audioClip.SetData(audioSamples, 0);
            audioSource.clip = audioClip;
            audioSource.Play();
            _audioTracks.Add(audioSource);
        }

        DisposeAudioData(audioDataHandle);
    }

    private unsafe void ReadFrame()
    {
        fixed (Color32* framePtr = _frameBuffer)
        {
            GetFrameData(_videoFileHandle, framePtr, (uint)_frameBuffer.Length);
        }
    }

    private IEnumerator PlayVideo()
    {
        while (_videoFileHandle != IntPtr.Zero)
        {
            _videoTimer += Time.deltaTime;
            while (_videoTimer > 0f)
            {
                ReadFrame();

                _outputTexture.SetPixels32(_frameBuffer);
                _outputTexture.Apply(false, false);

                if (!AdvanceFrame(_videoFileHandle))
                {
                    OutputImage.texture = Texture2D.blackTexture;
                    Cleanup();
                }

                _videoTimer -= _secondsPerFrame;
            }

            yield return null;
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (_outputTexture != null)
        {
            Destroy(_outputTexture);
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
                Destroy(_audioTracks[i].clip);
                Destroy(_audioTracks[i]);
            }
            _audioTracks = null;
        }

        _frameBuffer = null;
    }
}
