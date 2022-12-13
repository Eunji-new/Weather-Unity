using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public Text date;

    [Space(10f)]
    public Text temperature;
    public Text sky;
    public Text rainType;

    [Space(10f)]
    public Text dustValue;
    public Text dustGrade;
    public Text debug_text;

    [Space(10f)]
    public GameObject Debug_Panel;
    Coroutine power_coroutine;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        date.text = GetDate();
    }

    public string GetDate()
    {
        string month = "";

        switch (DateTime.Now.ToString(("MM")))
        {
            case "01": month = "January"; break;
            case "02": month = "February"; break;
            case "03": month = "March"; break;
            case "04": month = "April"; break;
            case "05": month = "May"; break;
            case "06": month = "June"; break;
            case "07": month = "July"; break;
            case "08": month = "August"; break;
            case "09": month = "September"; break;
            case "10": month = "October"; break;
            case "11": month = "November"; break;
            case "12": month = "December"; break;
        }

        string day = DateTime.Now.ToString(("dd"));
        int day_number = int.Parse(day);

        if (day_number < 10)
        {
            day = day.Replace("0", "");
        }

        return "Today, " + day + " " + month;
    }

    public void SetWeather(string temperature, string sky, string rainType, string dustValue, string dustGrade)
    {
        this.temperature.text = temperature + "°";

        //SetWeatherImg(sky, rainType);
        this.sky.text = sky;

        this.rainType.text = rainType;
        this.dustValue.text = dustValue + " ㎍/㎥";
        this.dustGrade.text = dustGrade;
    }

    public void Log(string text)
    {
        Debug.Log(text);
        Debug_Panel.SetActive(true);
        Debug_Panel.GetComponent<CanvasGroup>().alpha = 1.0f;
        debug_text.text += text + "\n";
        StartCoroutine(FadeOut(Debug_Panel, 3.5f));
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Log("This is a TEST LOG");
        }
    }
    IEnumerator FadeIn(GameObject obj, float fadeTime)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (!cg) yield break;

        obj.SetActive(true);

        while (cg.alpha < 1)
        {
            cg.alpha += Time.deltaTime / fadeTime;
            yield return null;
        }
    }

    IEnumerator FadeOut(GameObject obj, float fadeTime)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (!cg) yield break;

        while (cg.alpha > 0)
        {
            cg.alpha -= Time.deltaTime / fadeTime;
            yield return null;
        }

        obj.SetActive(false);
    }

    IEnumerator LerpUI(GameObject obj, Vector2 start, Vector2 end)
    {
        Vector2 cur = start;
        while (Vector2.Distance(cur, end) > 0.1f)
        {
            yield return new WaitForEndOfFrame();
            cur = Vector2.Lerp(cur, end, Time.deltaTime * 3);
            obj.GetComponent<RectTransform>().anchoredPosition = cur;
        }
        yield return null;
    }
}
