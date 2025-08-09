using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class RecorderManager : MonoBehaviour
{
    public bool IsRecording { get; private set; }
    public bool IsCompleted { get; private set; }   // ����ڰ� �����߰ų� ���ε� �Ϸ�

    public string SavedFilePath { get; private set; }
    public string SttResultText { get; private set; }  // ������ ��� �� ��

    private AudioClip _clip;
    private string _micDevice;
    private const int SampleRate = 16000; // ���� 16kHz (STT ���� �� ȣȯ ����)

    public void BeginRecord()
    {
        if (IsRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("����ũ ����̽��� ã�� ���߽��ϴ�.");
            return;
        }

        IsCompleted = false;
        _micDevice = Microphone.devices[0];
        // lengthSec=60�� �ִ� ���� ���� ���� (1��). ���� ������ Stop���� clip.length �������� �����.
        _clip = Microphone.Start(_micDevice, false, 60, SampleRate); // 5�� ����, 16kHz ����

        if (_clip == null)
        {
            Debug.LogError("����ũ ���� ����");
            return;
        }

        IsRecording = true;
        Debug.Log($"���� ����: device={_micDevice}, rate={SampleRate}Hz");
    }

    public IEnumerator StopAndProcess()
    {
        if (!IsRecording) yield break;

        // ���� ���� & ���� ����
        Microphone.End(_micDevice);
        IsRecording = false;

        // ������ ������ ���۵��� ���� �����ӿ��� ��� Stop�Ǹ� length�� 0�� �� �־�, �� ������ �纸
        yield return null;

        if (_clip == null || _clip.length <= 0f)
        {
            Debug.LogWarning("���� ���̰� 0�Դϴ�. ������ �ǳʶݴϴ�.");
            IsCompleted = true;
            yield break;
        }

        // ������Ʈ ��Ʈ(Assets ���� ����)�� "Recordings" ���� ����
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string saveFolder = Path.Combine(projectRoot, "Recordings");

        // ���� ������ ����
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        // ���� ���
        string fileName = $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        SavedFilePath = Path.Combine(saveFolder, fileName);

        try
        {
            SaveWav(SavedFilePath, _clip);
            Debug.Log($"WAV ���� �Ϸ�: {SavedFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"WAV ���� ����: {e.Message}");
        }

        // TODO: STT ���� ���ε� & ��� ����
        // yield return StartCoroutine(UploadAndTranscribe(SavedFilePath));
        // (STT�� ���߿�) SttResultText = null;

        IsCompleted = true;
    }

    /// <summary>
    /// AudioClip �� 16-bit PCM WAV�� ����
    /// </summary>
    private void SaveWav(string path, AudioClip clip)
    {
        // ���� ����
        int samples = clip.samples * clip.channels;
        float[] data = new float[samples];
        clip.GetData(data, 0);

        // float(-1~1) �� PCM16 ��ȯ
        byte[] pcm16 = new byte[samples * 2];
        int pcmIndex = 0;
        for (int i = 0; i < samples; i++)
        {
            float f = Mathf.Clamp(data[i], -1f, 1f);
            short s = (short)Mathf.RoundToInt(f * short.MaxValue);
            pcm16[pcmIndex++] = (byte)(s & 0xFF);
            pcm16[pcmIndex++] = (byte)((s >> 8) & 0xFF);
        }

        // WAV ��� + ������ �ۼ�
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

            // RIFF ���
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt ����ûũ
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);                // Subchunk1Size for PCM
            bw.Write((short)1);          // AudioFormat = 1 (PCM)
            bw.Write(channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write(bitsPerSample);

            // data ����ûũ
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);
            bw.Write(pcm16);
        }
    }
}