using System.Collections;
using System;
using System.Net;
using System.IO;
using UnityEngine;
using LitJson;

public class LatXLonY
{
    public float lat;
    public float lon;

    public float x;
    public float y;
}

public class DustInfo
{
    public string value;
    public string grade;

    public DustInfo() { }

    public DustInfo(string _v, string _g)
    {
        this.value = _v;
        this.grade = _g;
    }
}

public class Weather
{
    public string temperature;
    public string sky;
    public string rainType;
    public string dustValue;
    public string dustGrade;

    public Weather() { }

    public Weather(string t, string s, string r, string dv, string dg)
    {
        this.temperature = t;
        this.sky = s;
        this.rainType = r;
        this.dustValue = dv;
        this.dustGrade = dg;
    }
}

public class TM
{
    public string x;
    public string y;

    public TM() { }
    public TM(string _x, string _y)
    {
        this.x = _x;
        this.y = _y;
    }
}


public class WeatherManager : MonoBehaviour
{
    public float lat = 36f; //위도
    public float lon = 127f; //경도

    bool isSuccessAPI = false;
    string ERROR = "error";
    bool isUpdated = false;
    int updateMin = 45;

    void Start()
    {
        StartCoroutine(LoadWeather(lat, lon));
        StartCoroutine(IsCheckWeatherTime());
    }

    IEnumerator IsCheckWeatherTime()
    {
        yield return new WaitForSeconds(60);

        string min = DateTime.Now.ToString("mm");
        float _min = float.Parse(min);

        //날씨 API 업데이트 시간이면 날씨 로딩(매 시간 45분)
        if(_min == updateMin && isUpdated == false)
        {
            StartCoroutine(LoadWeather(lat, lon));
            //UIManager.instance.Log("날씨 API 업데이트 시간");
            isUpdated = true;
        }
        else
        {
            //UIManager.instance.Log("날씨 API 업데이트 시간 아님");
            isUpdated = false;
        }

        StartCoroutine("IsCheckWeatherTime");
    }

    IEnumerator LoadWeather(float lat, float lon) //json 문자열 받아오기
    {
        isSuccessAPI = false;

        //현재 날짜 및 시간
        string currentDate = DateTime.Now.ToString(("yyyyMMdd"));
        string currentTime = DateTime.Now.ToString(("HHmm"));
        string baseTime = SetBaseTime(currentTime);

        while (!isSuccessAPI)
        {
            isSuccessAPI = true;

            Weather wt = GetWeather(lat, lon, currentDate, baseTime);
            //UIManager.instance.Log("날씨 API 연동 성공 여부 : " + isSuccessAPI);
            if (isSuccessAPI)
            {    
                UIManager.instance.SetWeather(wt.temperature, wt.sky, wt.rainType, wt.dustValue, wt.dustGrade);
                yield break;
            }
            //성공 못하면 baseTime 재설정. 1초 이후에 다시 API 정보 받아오기
            baseTime = SetBaseTime(baseTime);
            yield return new WaitForSeconds(1.0f);
        }

        yield return null;

    }

    public Weather GetWeather(float lat, float lon, string currentDate, string baseTime)
    {
        Weather weather = new Weather(ERROR, ERROR, ERROR, ERROR, ERROR);

        //위도, 경도 바탕으로 격자 x,y좌표
        LatXLonY Grid = dfs_xy_conv(lat, lon);

        //초단기 실황 URL
        string GetWeatherUrl = "http://apis.data.go.kr/1360000/VilageFcstInfoService_2.0/getUltraSrtNcst?serviceKey=인증키&numOfRows=1000&pageNo=1&dataType=json&base_date=" + currentDate + "&base_time=" + baseTime + "&nx=" + Grid.x.ToString() + "&ny=" + Grid.y.ToString();
        //초단기 예보 URL
        string GetSkyUrl = "http://apis.data.go.kr/1360000/VilageFcstInfoService_2.0/getUltraSrtFcst?serviceKey=인증키&numOfRows=1000&pageNo=1&dataType=json&base_date=" + currentDate + "&base_time=" + baseTime + "&nx=" + Grid.x.ToString() + "&ny=" + Grid.y.ToString();
        
        //Debug.Log(GetWeatherUrl);

        string resultsWeather = GetJsonResult(GetWeatherUrl); //날씨 정보 json(온도, 강수)
        string resultsSky = GetJsonResult(GetSkyUrl); //하늘 정보 json 
        Debug.Log(resultsWeather);
        Debug.Log(resultsSky);

        weather.temperature = GetTemperature(resultsWeather);
        weather.rainType = GetRainType(resultsWeather);
        weather.sky = GetSky(resultsSky);

        //미세먼지 측정
        DustInfo dust = GetDustInfo(lat, lon);
        weather.dustValue = dust.value;
        weather.dustGrade = dust.grade;

        return weather;
    }

    public DustInfo GetDustInfo(float lat, float lon)
    {
        DustInfo dust = new DustInfo(ERROR, ERROR);
        
        //accessToken 얻기
        string GetAccessTokenUrl = "https://sgisapi.kostat.go.kr/OpenAPI3/auth/authentication.json?consumer_key=서비스ID&consumer_secret=보안Key"; //인증키 입력
        string resultsAccessToken = GetJsonResult(GetAccessTokenUrl); //access Token 정보 json(TM좌표 변환에 사용)
        string accessToken = GetAccessToken(resultsAccessToken);

        //위도 경도 바탕으로 TM좌표 얻기 x축이 경도, y축이 위도
        string GetTMUrl = "https://sgisapi.kostat.go.kr/OpenAPI3/transformation/transcoord.json?accessToken=" + accessToken + "&src=EPSG:4326&dst=EPSG:5181&posX=" + lon + "&posY=" + lat;
        string resultsTM = GetJsonResult(GetTMUrl); //TM 좌표 json
        TM tm = GetTM(resultsTM);

        if (tm.x == ERROR || tm.y == ERROR) return dust;

        //TM좌표로 제일가까운 측정소 이름 얻기
        string GetDustPosUrl = "http://apis.data.go.kr/B552584/MsrstnInfoInqireSvc/getNearbyMsrstnList?tmX=" + tm.x + "&tmY=" + tm.y + "&returnType=json&serviceKey=인증키";
        string resultsDustPos = GetJsonResult(GetDustPosUrl); //미세먼지 측정소 정보 json(미세먼지 측정에 사용)
        string dustPos = GetDustPos(resultsDustPos);

        if (dustPos == ERROR) return dust;

        //측정소 이름으로 미세먼지 정보 얻기
        string GetDustUrl = "http://apis.data.go.kr/B552584/ArpltnInforInqireSvc/getMsrstnAcctoRltmMesureDnsty?stationName=" + dustPos + "&dataTerm=daily&pageNo=1&numOfRows=10000&returnType=json&serviceKey=인증키";
        string resultsDust = GetJsonResult(GetDustUrl); //미세먼지 정보 json

        dust.value = GetDustValue(resultsDust);
        dust.grade = GetDustGrade(resultsDust);

        return dust;
    }
    //BaseTime 설정 : 현재시간이 11시 20분이면 11시의 API 업데이트가 되지 않은 상태. 10시 xx분으로 바꿔줘야한다.
    string SetBaseTime(string time) //HHmm 받아온다.
    {
        int hh = int.Parse(time.Substring(0,2));
        int mm = int.Parse(time.Substring(2,2));

        if (mm < updateMin)
        {
            hh--;
            if (hh < 0)
            {
                hh = 24 + hh;
            }
        }
        
        string HH = hh.ToString();
        string MM = mm.ToString();

        if(hh < 10)
            HH = "0"+ hh.ToString();
        if(mm < 10)
            MM = "0" + mm.ToString();

        return HH + MM;
    }

    string GetJsonResult(string url)
    {
        string GetUrl = url;
        string results;
        var request = (HttpWebRequest)WebRequest.Create(GetUrl);

        request.Method = "GET";

        HttpWebResponse response;

        using (response = request.GetResponse() as HttpWebResponse)
        {
            StreamReader reader = new StreamReader(response.GetResponseStream());
            results = reader.ReadToEnd();
        }
        //Debug.Log(results);

        if(results[0] == '<')//xml로 받아와지면 다시
            results = GetJsonResult(url);

        return results;
    }

    string GetAccessToken(string resultsAccessToken)
    {
        JsonData AccessTokenData = JsonMapper.ToObject(resultsAccessToken);
        if (AccessTokenData["errCd"].ToString() == "-401")
        {
            //UIManager.instance.Log("[ACCESSTOKEN]API가 정상적으로 호출되지 않음");
            isSuccessAPI = false;
            return ERROR;
        }
        string accessToken = AccessTokenData["result"]["accessToken"].ToString();

        return accessToken;
    }

    TM GetTM(string resultsTM)
    {
        TM tm = new TM(ERROR, ERROR);
        JsonData TMData = JsonMapper.ToObject(resultsTM);
        if (TMData["errCd"].ToString() == "-401")
        {
            //UIManager.instance.Log("[TM]API가 정상적으로 호출되지 않음");
            isSuccessAPI = false;
            return tm;
        }
        tm.x = TMData["result"]["posX"].ToString();
        tm.y = TMData["result"]["posY"].ToString();

        return tm;
    }

    string GetDustPos(string resultsDustPos)
    {
        JsonData dustPosData = JsonMapper.ToObject(resultsDustPos);

        if (dustPosData["response"]["header"]["resultCode"].ToString() != "00")
        {
            //UIManager.instance.Log("[측정소 위치]API가 정상적으로 호출되지 않음");
            isSuccessAPI = false;
            return ERROR;
        }

        string dustPos = dustPosData["response"]["body"]["items"][0]["stationName"].ToString(); //0번째가 가장 가까운 측정소

        //Debug.Log("미세먼지 측정소 위치 : " + dustPos);

        return dustPos;

    }

    string GetDustValue(string resultsDust)
    {
        JsonData dustData = JsonMapper.ToObject(resultsDust);

        if (dustData["response"]["header"]["resultCode"].ToString() != "00")
        {
            //UIManager.instance.Log("[미세먼지 수치]API가 정상적으로 호출되지 않음");
            isSuccessAPI = false;
            return ERROR;
        }
        string dustValue = dustData["response"]["body"]["items"][0]["pm10Value"].ToString();

        //Debug.Log("미세먼지 : " + dustValue );

        return dustValue;

    }

    string GetDustGrade(string resultsDust)
    {
        JsonData dustData = JsonMapper.ToObject(resultsDust);
        if (dustData["response"]["header"]["resultCode"].ToString() != "00")
        {
            //UIManager.instance.Log("[미세먼지 정보]API가 정상적으로 호출되지 않음");
            isSuccessAPI = false;
            return ERROR;
        }
        string dustGrade = dustData["response"]["body"]["items"][0]["pm10Grade"].ToString();
        switch (dustGrade)
        {
            case "1": dustGrade = "좋음"; break;
            case "2": dustGrade = "보통"; break;
            case "3": dustGrade = "나쁨"; break;
            case "4": dustGrade = "매우나쁨"; break;
            default: dustGrade = "보통"; isSuccessAPI = false; break;
        }

        //Debug.Log("미세먼지 정도 : " + dustGrade +"(좋음 :1, 보통 : 2, 나쁨 : 3, 매우나쁨 : 4)");

        return dustGrade;
    }

    string GetTemperature(string resultsWeather)
    {
        JsonData weatherData = JsonMapper.ToObject(resultsWeather);
        if (weatherData["response"]["header"]["resultCode"].ToString() != "00")
        {
            //Debug.Log("[온도]API가 정상적으로 호출되지 않음");
            //DebugLog.instance.Log("[온도]" + weatherData["response"]["header"]["resultMsg"].ToString());
            isSuccessAPI = false;
            return ERROR;
        }
        string temperature = weatherData["response"]["body"]["items"]["item"][3]["obsrValue"].ToString(); //item 3번째가 "T1H" - 온도

        float _temp = float.Parse(temperature);

        if (_temp <= -900 || _temp >= 900)
        {
            temperature = "20";
            isSuccessAPI = false;
        }
        //Debug.Log("온도 : " + temperature);

        float temp_number = float.Parse(temperature); //소수점 첫째자리 반올림
        
        return Mathf.Round(temp_number).ToString();
    }

    string GetSky(string resultsSky)
    {
        JsonData skyData = JsonMapper.ToObject(resultsSky);
        if (skyData["response"]["header"]["resultCode"].ToString() != "00")
        {
            //UIManager.instance.Log("[하늘]API가 정상적으로 호출되지 않음");
            //DebugLog.instance.Log("[하늘]" + skyData["response"]["header"]["resultMsg"].ToString());
            isSuccessAPI = false;
            return ERROR;
        }
        string sky = skyData["response"]["body"]["items"]["item"][18]["fcstValue"].ToString(); //18번째가 가장 최근 sky

        switch (sky)
        {
            case "1": sky = "맑음"; break;
            case "3": sky = "구름많음"; break;
            case "4": sky = "흐림"; break;
            default: sky = "맑음"; isSuccessAPI = false; break;
        }
        //Debug.Log("날씨 : " + sky + " (맑음(1), 구름많음(3), 흐림(4)) ");

        return sky;
    }

    string GetRainType(string resultsWeather)
    {
        JsonData weatherData = JsonMapper.ToObject(resultsWeather);
        if (weatherData["response"]["header"]["resultCode"].ToString() != "00")
        {
            //UIManager.instance.Log("[강수형태] API가 정상적으로 호출되지 않음");
            //DebugLog.instance.Log("[강수형태]" + weatherData["response"]["header"]["resultMsg"].ToString());
            isSuccessAPI = false;
            return ERROR;
        }
        string rainType = weatherData["response"]["body"]["items"]["item"][0]["obsrValue"].ToString();//0번째가 PTY

        switch (rainType)
        {
            case "0": rainType = "비 안옴"; break;
            case "1": rainType = "비"; break;
            case "2": rainType = "비눈"; break;
            case "3": rainType = "눈"; break;
            case "5": rainType = "빗방울"; break;
            case "6": rainType = "빗방울눈날림"; break;
            case "7": rainType = "눈날림"; break;
            default: rainType = "비 안옴"; isSuccessAPI = false; break;
        }
        //Debug.Log("강수형태 : " + rainType + " (없음(0), 비(1), 비/눈(2), 눈(3), 빗방울(5), 빗방울눈날림(6), 눈날림(7) )");

        return rainType;
    }

    //위경도로 xy계산
    float RE = 6371.00877f; // 지도반경
    float GRID = 5.0f;   // 격자간격 (km)
    float SLAT1 = 30.0f;   // 표준위도 1
    float SLAT2 = 60.0f;   // 표준위도 2
    float OLON = 126.0f;  // 기준점 경도
    float OLAT = 38.0f; // 기준점 위도
    float XO = 43.0f;  // 기준점 X좌표
    float YO = 136.0f; // 기준점 Y좌표

    public LatXLonY dfs_xy_conv(float _dLat, float _dLon)
    {
        float DEGARD = Mathf.PI / 180.0f;
        //float RADDEG = 180.0 / Mathf.PI;

        float re = RE / GRID;
        float slat1 = SLAT1 * DEGARD;
        float slat2 = SLAT2 * DEGARD;
        float olon = OLON * DEGARD;
        float olat = OLAT * DEGARD;

        float sn = Mathf.Tan(Mathf.PI * 0.25f + slat2 * 0.5f) / Mathf.Tan(Mathf.PI * 0.25f + slat1 * 0.5f);
        sn = Mathf.Log(Mathf.Cos(slat1) / Mathf.Cos(slat2)) / Mathf.Log(sn);
        float sf = Mathf.Tan(Mathf.PI * 0.25f + slat1 * 0.5f);
        sf = Mathf.Pow(sf, sn) * Mathf.Cos(slat1) / sn;
        float ro = Mathf.Tan(Mathf.PI * 0.25f + olat * 0.5f);
        ro = re * sf / Mathf.Pow(ro, sn);

        LatXLonY rs = new LatXLonY();
        rs.lat = _dLat;
        rs.lon = _dLon;

        float ra = Mathf.Tan(Mathf.PI * 0.25f + _dLat * DEGARD * 0.5f);
        ra = re * sf / Mathf.Pow(ra, sn);
        float theta = _dLon * DEGARD - olon;
        if (theta > Mathf.PI) theta -= 2.0f * Mathf.PI;
        if (theta < -Mathf.PI) theta += 2.0f * Mathf.PI;
        theta *= sn;
        rs.x = Mathf.Floor(ra * Mathf.Sin(theta) + XO + 0.5f);
        rs.y = Mathf.Floor(ro - ra * Mathf.Cos(theta) + YO + 0.5f);

        return rs;
    }

}
