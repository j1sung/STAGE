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

    // Inspector ��ư���� ȣ���� �� �ֵ��� �ʵ�/�޼��� �߰�
    private Action _stopAction; // ���� ������ stopAction�� ����
    private bool _awaitingAnswer;  // ��ư ��Ÿ ����/���� Ȯ�ο�

    void Start()
    {
        StartCoroutine(InterviewSequence());
    }


    // ���� ���� ��ư ����
    public void OnClickStopRecord()
    {
        // ���� ���� ó�� ���� ���� ����
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
        // ȭ�� ǥ��
        questionText.text = text;

        //TTS ��� �� ���
        yield return StartCoroutine(tts.Speak(text));

        if (skipRecord) yield break;

        // Ÿ�̸� + ���� ���� ����
        bool timeUp = false;
        bool userDone = false;
        _awaitingAnswer = true;

        // Ÿ�̸�
        Coroutine timerCo = StartCoroutine(Timer(timeoutSec, () => timeUp = true));

        // ���� ����
        recorder.BeginRecord();

        // ���� ��ư ����
        _stopAction = () =>
        {
            if (!userDone)
            {
                userDone = true;
                if(timerCo != null) StopCoroutine(timerCo);
                StartCoroutine(recorder.StopAndProcess());
            }
        };

        // Ÿ�Ӿƿ� or ���� �Ϸ���� ���
        yield return new WaitUntil(() => timeUp || recorder.IsCompleted);

        // ���� ���������� ���� ������
        if (timeUp)
        {
            // Ÿ�Ӿƿ��̸� ���� �ߴ� ����
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

        // ���� ���� �� �������� �ʱ�ȭ
        _awaitingAnswer = false;
        _stopAction = null; // ���� ����
    }

    IEnumerator Timer(float seconds, Action action) 
    {
        debugTimeLeft = seconds;
        float t = seconds;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            debugTimeLeft = Mathf.Max(0f, t); // GUI ǥ�ô� 0 ���Ϸ� �� ��������
            yield return null;
        }
        action?.Invoke(); // �ð��� �� �Ǹ� timeUp = true ����
    }
    void OnGUI()
    {
        if (_awaitingAnswer)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black; // ���� ���� ��������
            style.fontSize = 20; // ���� ũ�� ���� ����

            GUI.Label(new Rect(10, 10, 220, 30),
                      $"���� �ð�: {debugTimeLeft:F1}��",
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
