using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class RecorderManager : MonoBehaviour
{
    public bool IsRecording { get; private set; }
    public bool IsCompleted { get; private set; }   // 사용자가 종료했거나 업로드 완료

    public string SavedFilePath { get; private set; }
    public string SttResultText { get; private set; }  // 지금은 사용 안 함

    private AudioClip _clip;
    private string _micDevice;
    private const int SampleRate = 16000; // 예시 16kHz (STT 연동 시 호환 좋음)

    public void BeginRecord()
    {
        if (IsRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("마이크 디바이스를 찾지 못했습니다.");
            return;
        }

        IsCompleted = false;
        _micDevice = Microphone.devices[0];
        // lengthSec=60은 최대 녹음 길이 버퍼 (1분). 실제 저장은 Stop에서 clip.length 기준으로 저장됨.
        _clip = Microphone.Start(_micDevice, false, 60, SampleRate); // 5분 버퍼, 16kHz 예시

        if (_clip == null)
        {
            Debug.LogError("마이크 시작 실패");
            return;
        }

        IsRecording = true;
        Debug.Log($"녹음 시작: device={_micDevice}, rate={SampleRate}Hz");
    }

    public IEnumerator StopAndProcess()
    {
        if (!IsRecording) yield break;

        // 녹음 정지 & 파일 저장
        Microphone.End(_micDevice);
        IsRecording = false;

        // 녹음이 실제로 시작되지 않은 프레임에서 즉시 Stop되면 length가 0일 수 있어, 한 프레임 양보
        yield return null;

        if (_clip == null || _clip.length <= 0f)
        {
            Debug.LogWarning("녹음 길이가 0입니다. 저장을 건너뜁니다.");
            IsCompleted = true;
            yield break;
        }

        // 프로젝트 루트(Assets 폴더 상위)에 "Recordings" 폴더 생성
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string saveFolder = Path.Combine(projectRoot, "Recordings");

        // 폴더 없으면 생성
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        // 파일 경로
        string fileName = $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        SavedFilePath = Path.Combine(saveFolder, fileName);

        try
        {
            SaveWav(SavedFilePath, _clip);
            Debug.Log($"WAV 저장 완료: {SavedFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"WAV 저장 실패: {e.Message}");
        }

        // TODO: STT 서버 업로드 & 결과 수신
        // yield return StartCoroutine(UploadAndTranscribe(SavedFilePath));
        // (STT는 나중에) SttResultText = null;

        IsCompleted = true;
    }

    /// <summary>
    /// AudioClip → 16-bit PCM WAV로 저장
    /// </summary>
    private void SaveWav(string path, AudioClip clip)
    {
        // 샘플 추출
        int samples = clip.samples * clip.channels;
        float[] data = new float[samples];
        clip.GetData(data, 0);

        // float(-1~1) → PCM16 변환
        byte[] pcm16 = new byte[samples * 2];
        int pcmIndex = 0;
        for (int i = 0; i < samples; i++)
        {
            float f = Mathf.Clamp(data[i], -1f, 1f);
            short s = (short)Mathf.RoundToInt(f * short.MaxValue);
            pcm16[pcmIndex++] = (byte)(s & 0xFF);
            pcm16[pcmIndex++] = (byte)((s >> 8) & 0xFF);
        }

        // WAV 헤더 + 데이터 작성
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            int sampleRate = clip.frequency;
            short channels = (short)clip.channels;
            short bitsPerSample = 16;
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            short blockAlign = (short)(channels * (bitsPerSample / 8));
            int subchunk2Size = pcm16.Length;
            int chunkSize = 36 + subchunk2Size;

            // RIFF 헤더
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt 서브청크
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);                // Subchunk1Size for PCM
            bw.Write((short)1);          // AudioFormat = 1 (PCM)
            bw.Write(channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write(bitsPerSample);

            // data 서브청크
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);
            bw.Write(pcm16);
        }
    }
}