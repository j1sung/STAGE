using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterviewManager : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public GameObject panel;

    [TextArea(2, 5)]
    public List<string> startQuestions;
    [TextArea(2, 5)]
    public List<string> mainQuestions;
    [TextArea(2, 5)]
    public List<string> endQuestions;

    void Start()
    {
        StartCoroutine(InterviewSequence());
    }

    IEnumerator InterviewSequence()
    {
        panel.SetActive(true);

        questionText.text = GetRandomQuestion(startQuestions);
        yield return new WaitForSeconds(2f);

        List<string> selectedMain = GetRandomSubset(mainQuestions, Random.Range(3, 5));
        foreach (string q in selectedMain)
        {
            questionText.text = q;
            yield return new WaitForSeconds(5f);
        }

        questionText.text = GetRandomQuestion(endQuestions);
        yield return new WaitForSeconds(2f);

        questionText.text = "감사합니다.";
        yield return new WaitForSeconds(3f);
    }

    string GetRandomQuestion(List<string> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    List<string> GetRandomSubset(List<string> list, int count)
    {
        List<string> copy = new List<string>(list);
        List<string> result = new List<string>();

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, copy.Count);
            result.Add(copy[idx]);
            copy.RemoveAt(idx);
        }

        return result;
    }
}
