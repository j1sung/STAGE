using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class InterviewManagerJS : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public GameObject panel;

    public AudioSource sfx;
    public AudioClip bellClip;

    public TTSManager1 tts;
    public RecorderManager recorder;

    [TextArea(2, 5)]
    public List<string> startQuestions;
    [TextArea(2, 5)]
    public List<string> mainQuestions;
    [TextArea(2, 5)]
    public List<string> endQuestions;
    [TextArea(2, 5)]
    public string ending;


    public float defaultTimeout = 5f;
    private float debugTimeLeft;

    // Inspector 버튼에서 호출할 수 있도록 필드/메서드 추가
    private Action _stopAction; // 현재 질문용 stopAction을 보관
    private bool _awaitingAnswer;  // 버튼 연타 방지/상태 확인용

    void Start()
    {
        StartCoroutine(InterviewSequence());
    }


    // 녹음 종료 버튼 연결
    public void OnClickStopRecord()
    {
        // 현재 질문 처리 중일 때만 동작
        if (_awaitingAnswer)
        {
            _stopAction?.Invoke();
        }
    }

    IEnumerator InterviewSequence()
    {
        panel.SetActive(true);

        string startQ = GetRandomQuestion(startQuestions);
        yield return StartCoroutine(AskOne(startQ, defaultTimeout));

        List<string> selectedMain = GetRandomSubset(mainQuestions, UnityEngine.Random.Range(3, 5));
        foreach (string q in selectedMain)
        {
            yield return StartCoroutine(AskOne(q, defaultTimeout));
        }

        string endQ = GetRandomQuestion(endQuestions);
        yield return StartCoroutine(AskOne(endQ, defaultTimeout));

        yield return StartCoroutine(AskOne(ending, 2f, skipRecord:true ));
        SceneManager.LoadScene("End");
    }

    IEnumerator AskOne(string text, float timeoutSec, bool skipRecord = false)
    {
        // 화면 표시
        questionText.text = text;

        //TTS 재생 및 대기
        yield return StartCoroutine(tts.Speak(text));

        if (skipRecord) yield break;

        // 타이머 + 녹음 병렬 시작
        bool timeUp = false;
        bool userDone = false;
        _awaitingAnswer = true;

        // 타이머
        Coroutine timerCo = StartCoroutine(Timer(timeoutSec, () => timeUp = true));

        // 녹음 시작
        recorder.BeginRecord();

        // 종료 버튼 동작
        _stopAction = () =>
        {
            if (!userDone)
            {
                userDone = true;
                if(timerCo != null) StopCoroutine(timerCo);
                StartCoroutine(recorder.StopAndProcess());
            }
        };

        // 타임아웃 or 녹음 완료까지 대기
        yield return new WaitUntil(() => timeUp || recorder.IsCompleted);

        // 누가 먼저인지에 따라 마무리
        if (timeUp)
        {
            // 타임아웃이면 녹음 중단 정리
            if (recorder.IsRecording)
            {
                yield return StartCoroutine(recorder.StopAndProcess());
            }
        }

        if (sfx && bellClip)
        { 
            sfx.PlayOneShot(bellClip);
            yield return new WaitWhile(() => sfx.isPlaying);
        }

        // 다음 질문 전 전역변수 초기화
        _awaitingAnswer = false;
        _stopAction = null; // 누수 방지
    }

    IEnumerator Timer(float seconds, Action action) 
    {
        debugTimeLeft = seconds;
        float t = seconds;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            debugTimeLeft = Mathf.Max(0f, t); // GUI 표시는 0 이하로 안 내려가게
            yield return null;
        }
        action?.Invoke(); // 시간이 다 되면 timeUp = true 실행
    }
    void OnGUI()
    {
        if (_awaitingAnswer)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black; // 글자 색을 검정으로
            style.fontSize = 20; // 글자 크기 조절 가능

            GUI.Label(new Rect(10, 10, 220, 30),
                      $"남은 시간: {debugTimeLeft:F1}초",
                      style);
        }
    }


    string GetRandomQuestion(List<string> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    List<string> GetRandomSubset(List<string> list, int count)
    {
        List<string> copy = new List<string>(list);
        List<string> result = new List<string>();

        for (int i = 0; i < count; i++)
        {
            int idx = UnityEngine.Random.Range(0, copy.Count);
            result.Add(copy[idx]);
            copy.RemoveAt(idx);
        }

        return result;
    }
}
