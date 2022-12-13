# Weather-Unity
Unity에서 날씨 공공데이터 API 사용하여 현재 날씨와 미세먼지 농도 가져오기
<br><br>

## 날씨🌞

[기상청_단기예보 ((구)_동네예보) 조회서비스](https://www.data.go.kr/data/15084084/openapi.do) 오픈 API사용

<br>

## 미세먼지 ☁

날씨를 알고싶은 위치의 **위경도 값** 필요

TM좌표를 알고 있다면 3번부터 시작
<br><br>

1. [개발지원센터](https://sgis.kostat.go.kr/developer/html/main.html)에서 인증키 신청 -> 서비스ID, 보안 Key를 받는다.
![image](https://user-images.githubusercontent.com/28985207/207206633-d7b63b07-66d5-4c00-8a41-80adcc2d3d26.png)

2. 1에서 받은 Key와 위경도를 입력하여 TM좌표를 얻음

   [좌표변환 API](https://sgis.kostat.go.kr/developer/html/newOpenApi/api/dataApi/coord.html)
3. TM좌표 기반으로 측정소 정보 얻음
    
    [한국환경공단_에어코리아_측정소정보 오픈 API](https://www.data.go.kr/data/15073877/openapi.do)
4. 측정소 이름으로 미세먼지 정보 얻음

    [한국환경공단_에어코리아_대기오염정보 오픈 API](https://www.data.go.kr/data/15073861/openapi.do)

<br>

## 인증키 넣기

다음과 같이 WeatherManager의 인스펙터값에 맞게 넣어주시면 됩니다.
- **weather_AuthKey** - 날씨 인증키
- **TM_serviceID** - 개발지원센터 서비스ID(TM좌표변환용)
- **TM_securityKey** - 개발지원센터 보안키(TM좌표변환용)
- **dustPos_AuthKey** - 미세먼지측정소 인증키
- **dust_AuthKey** - 대기오염정보 인증키

![image](https://user-images.githubusercontent.com/28985207/207208479-9613670f-ffd1-4f1b-9518-6f9429fce29e.png)

<br>

## 결과
![image](https://user-images.githubusercontent.com/28985207/207206575-be1f798c-0323-4125-b650-0f91307cb4e9.png)