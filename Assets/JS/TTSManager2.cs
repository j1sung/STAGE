using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor.Experimental.GraphView;

public class TTSManager2 : MonoBehaviour
{
    [Header("Google AI Studio (Gemini)")]
    [SerializeField] string apiKey = "AIzaSyDaLWgD9hf9zQU9Nxlq_ZliuQknbDYHtSA";

    [Header("Audio")]
    public AudioSource audioSource;


    public void Speak(string text)
    { 
            StartCoroutine(RequestTTS(text));

    }

    IEnumerator RequestTTS(string inputText)
    {

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key={apiKey}";

        var postData = new GeminiTTSRequest
        {
            contents = new List<Content>
            {
                new Content
                {
                    parts = new List < Part >
                    {
                        new Part { text = inputText }
                    }
                }
            },
            generationConfig = new GenerationConfig
            {
                responseModalities = new string[] { "AUDIO" },
                speechConfig = new SpeechConfig
                {
                    voiceConfig = new VoiceConfig
                    {
                        prebuiltVoiceConfig = new PrebuiltVoiceConfig
                        {
                            voiceName = "Kore"
                        }
                    }
                }
            },
            model = "gemini-2.5-flash-preview-tts"
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
        TTSResponse response = JsonUtility.FromJson<TTSResponse>(responseJson);
        if (response?.candidates?[0]?.content?.parts?[0]?.inlineData?.data == null)
        {
            Debug.LogError("오디오 데이터를 받지 못했습니다. 서버 응답을 확인하세요: " + responseJson);
            yield break;
        }
        string base64Audio = response.candidates[0].content.parts[0].inlineData.data;
        byte[] audioBytes = Convert.FromBase64String(base64Audio);

        // AudioClip 생성
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "tts.wav");
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
    public class GeminiTTSRequest
    {
        public string model = "gemini-2.5-flash-preview-tts";
        public List<Content> contents;
        public GenerationConfig generationConfig;
    }


    [Serializable]
    public class GenerationConfig
    {
        public string[] responseModalities = new string[] { "AUDIO" };
        public SpeechConfig speechConfig;
    }

    [Serializable]
    public class SpeechConfig
    {
        public VoiceConfig voiceConfig;
    }

    [Serializable]
    public class VoiceConfig
    {
        public PrebuiltVoiceConfig prebuiltVoiceConfig;
    }

    [Serializable]
    public class PrebuiltVoiceConfig
    {
        public string voiceName = "Kore"; // 음색을 Kore로 설정
    }

    [Serializable]
    public class TTSResponse
    {
        public List<Candidate> candidates;
    }

    [Serializable]
    public class Candidate
    {
        public Content content;
    }

    [Serializable]
    public class Content
    {
        public List<Part> parts;
    }

    [Serializable]
    public class Part
    {
        public string text; // 요청 시 사용
        public InlineData inlineData; // 응답 시 사용
    }

    [Serializable]
    public class InlineData
    {
        public string mimeType;
        public string data;
    }

    

    
   
}
   