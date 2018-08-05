#pragma once

#include "smacker.h"

const int MaxAudioTracks = 7;

struct SmkAudioData
{
	unsigned char TrackMask;
	unsigned char AudioChannels[MaxAudioTracks];
	unsigned long AudioRate[MaxAudioTracks];
	unsigned char BitDepth[MaxAudioTracks];
	unsigned long AudioDataSize[MaxAudioTracks];
	unsigned char* AudioData[MaxAudioTracks];
};

extern "C"
{
	// Open an SMK file from disk - return video width, height, frame count and microseconds between frames.
	__declspec(dllexport) void* OpenFile(const char* filePath, int& width, int& height, int& frameCount, float& microSecondsPerFrame);

	// Clean up SMK file.
	__declspec(dllexport) void DisposeFile(smk smkFileHandle);

	// Fill Color32 buffer with current frame's data.
	__declspec(dllexport) void GetFrameData(smk smkFileHandle, unsigned char* buffer, unsigned int bufferSize);

	// Attempt to advance to the next frame, returns false if there are no more frames.
	__declspec(dllexport) bool AdvanceFrame(smk smkFileHandle);
	
	// Load raw audio data.
	__declspec(dllexport) void* GetAudioData(smk smkFileHandle);
	
	// Clean up audio data.
	__declspec(dllexport) void DisposeAudioData(SmkAudioData* audioData);
}