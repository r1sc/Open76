#include "UnityInterface.h"
#include <cstdlib>
#include <string.h>

void* OpenFile(const char* filePath, int& width, int& height, int& frameCount, float& microSecondsPerFrame)
{
	const smk smkFile = smk_open_file(filePath, SMK_MODE_DISK);
	if (smkFile != nullptr)
	{
		// Process first frame.
		smk_first(smkFile);

		unsigned long videoWidth, videoHeight, videoFrameCount;
		double msPerFrame;

		// Retrieve video file dimensions.
		smk_info_all(smkFile, nullptr, &videoFrameCount, &msPerFrame);
		smk_info_video(smkFile, &videoWidth, &videoHeight, nullptr);

		width = int(videoWidth);
		height = int(videoHeight);
		frameCount = int(videoFrameCount);
		microSecondsPerFrame = float(msPerFrame);
	}
	else
	{
		width = 0;
		height = 0;
		frameCount = 0;
		microSecondsPerFrame = 0.0f;
	}

	return smkFile;
}

void DisposeFile(smk smkFileHandle)
{
	if (smkFileHandle == nullptr)
	{
		return;
	}

	smk_close(smkFileHandle);
}

void GetFrameData(smk smkFileHandle, unsigned char* buffer, unsigned int bufferSize)
{
	if (smkFileHandle == nullptr)
	{
		return;
	}

	// Retrieve the palette and image.
	const unsigned char* frameData = smk_get_video(smkFileHandle);
	const unsigned char* palleteData = smk_get_palette(smkFileHandle);

	// Retrieve dimensions.
	unsigned long width, height;
	smk_info_video(smkFileHandle, &width, &height, nullptr);

	// Quick bounds check.
	if (bufferSize < width * height * 3)
	{
		return;
	}

	// Read pixel data for frame.
	unsigned long pixelIndex = 0;
	for (long h = height - 1; h >= 0; --h)
	{
		for (unsigned long w = 0; w < width; ++w)
		{
			const int frameIndex = frameData[(h * width) + w] * 3;
			buffer[pixelIndex++] = palleteData[frameIndex];
			buffer[pixelIndex++] = palleteData[frameIndex + 1];
			buffer[pixelIndex++] = palleteData[frameIndex + 2];
		}
	}
}

void* GetAudioData(smk smkFileHandle)
{
	if (smkFileHandle == nullptr)
	{
		return nullptr;
	}

	SmkAudioData* smkAudioData = new SmkAudioData();

	unsigned long frameCount;
	double msPerFrame;

	unsigned char audioChannels[MaxAudioTracks];
	unsigned char bitDepth[MaxAudioTracks];
	unsigned long audioRate[MaxAudioTracks];

	smk_info_all(smkFileHandle, nullptr, &frameCount, &msPerFrame);
	smk_info_audio(smkFileHandle, &smkAudioData->TrackMask, audioChannels, bitDepth, audioRate);
	smk_enable_all(smkFileHandle, smkAudioData->TrackMask);

	// Read total audio length.
	smk_first(smkFileHandle);
	for (unsigned long frame = 0; frame < frameCount; ++frame)
	{
		for (int i = 0; i < MaxAudioTracks; ++i)
		{
			smkAudioData->BitDepth[i] = bitDepth[i];
			smkAudioData->AudioChannels[i] = audioChannels[i];
			smkAudioData->AudioRate[i] = audioRate[i];
			if (smkAudioData->TrackMask & 1 << i)
			{
				smkAudioData->AudioDataSize[i] += smk_get_audio_size(smkFileHandle, i);
			}
		}
		smk_next(smkFileHandle);
	}

	// Allocate buffer for each track.
	for (int i = 0; i < MaxAudioTracks; ++i)
	{
		if (smkAudioData->TrackMask & 1 << i)
		{
			smkAudioData->AudioData[i] = new unsigned char[smkAudioData->AudioDataSize[i]];
		}
	}

	// Copy data to buffer.
	smk_first(smkFileHandle);
	unsigned long dataIndex = 0;
	for (unsigned long frame = 0; frame < frameCount; ++frame)
	{
		for (int i = 0; i < MaxAudioTracks; ++i)
		{
			if (smkAudioData->TrackMask & 1 << i)
			{
				const unsigned long sizeThisFrame = smk_get_audio_size(smkFileHandle, i);
				memcpy_s(smkAudioData->AudioData[i] + dataIndex, sizeThisFrame, smk_get_audio(smkFileHandle, i), sizeThisFrame);
				dataIndex += sizeThisFrame;
			}
		}
		smk_next(smkFileHandle);
	}

	// Enable video, then rewind to the start (order is important!)
	smk_enable_video(smkFileHandle, 1);
	smk_first(smkFileHandle);

	return smkAudioData;
}

void DisposeAudioData(SmkAudioData* audioData)
{
	if (audioData == nullptr)
	{
		return;
	}

	for (int i = 0; i < MaxAudioTracks; ++i)
	{
		if (audioData->TrackMask & 1 << i)
		{
			delete audioData->AudioData[i];
		}
	}

	delete audioData;
}

bool AdvanceFrame(smk smkFileHandle)
{
	if (smkFileHandle == nullptr)
	{
		return false;
	}

	return smk_next(smkFileHandle) != SMK_DONE;
}