using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TTSManager1 : MonoBehaviour
{
    [Header("Google Cloud TTS API")]
    [SerializeField] string apiKey = "AIzaSyA425Ty9NDIi1VMDsCDSaClkyOw7X9pA14";

    [Header("Audio")]
    public AudioSource audioSource;

    public IEnumerator Speak(string text)
    {
        yield return StartCoroutine(RequestTTS(text));

        // 재생 완료까지 대기
        yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
    }

    IEnumerator RequestTTS(string inputText)
    {
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

        var postData = new TTSRequest
        {
            input = new Input { text = inputText },
            voice = new Voice
            {
                languageCode = "ko-KR",
                name = "ko-KR-Standard-C"
            },
            audioConfig = new AudioConfig
            {
                audioEncoding = "LINEAR16"
            }
        };

        string jsonPayload = JsonUtility.ToJson(postData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"TTS 요청 실패: {request.error}");
            yield break;
        }

        // JSON에서 audioContent 추출
        string responseJson = request.downloadHandler.text;
        string base64Audio = JsonUtility.FromJson<TTSResponse>(responseJson).audioContent;
        byte[] audioBytes = Convert.FromBase64String(base64Audio);

        // 프로젝트 루트(Assets 밖) 경로 생성
        string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
        string saveFolder = System.IO.Path.Combine(projectRoot, "TTS_Audio");
        if (!System.IO.Directory.Exists(saveFolder))
            System.IO.Directory.CreateDirectory(saveFolder);

        // AudioClip 생성
        string filePath = System.IO.Path.Combine(saveFolder, "tts.wav");
        System.IO.File.WriteAllBytes(filePath, audioBytes);

        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
        {
            yield return audioRequest.SendWebRequest();
            if(audioRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Audio 로드 실패: {audioRequest.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    [Serializable]
    public class TTSRequest
    {
        public Input input;
        public Voice voice;
        public AudioConfig audioConfig;
    }


    [Serializable]
    public class Input
    {
        public string text;
    }

    [Serializable]
    public class Voice
    {
        public string languageCode;
        public string name;
    }

    [Serializable]
    public class AudioConfig
    {
        public string audioEncoding;
    }

    [Serializable]
    private class TTSResponse
    {
        public string audioContent;
    }
}
   