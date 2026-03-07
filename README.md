# 🏓 Multiplayer Pong Game

Unity Netcode for GameObjects(NGO)를 활용한 실시간 멀티플레이어 Pong 게임입니다.

## 🎮 Gameplay
스크린샷 추가 예정

## 📌 Features
- 실시간 멀티플레이어 Pong
- 아이템 시스템 (10초마다 랜덤 스폰)
- 라운드 리셋 시스템
- 점수 동기화

## 🛠 Tech Stack
- Unity
- C#
- Netcode for GameObjects (NGO)

## ⚙️ Key Systems

### Item System
- 10초마다 무작위 위치에 아이템 스폰
- 패들 크기 증가
- 공 크기 감소
- 공 속도 증가

### Round Reset System
- 득점 발생 시
  - 바닥 아이템 제거
  - 스폰 타이머 초기화

### Network Sync
- NetworkVariable을 이용한 점수 동기화
- ClientRpc를 통한 이벤트 동기화

## 🛠 Troubleshooting

### Item Spawn Timer Duplication (Ghost Coroutine)

**문제**
득점 후 스폰 타이머를 초기화했지만 1초 뒤 아이템이 스폰되는 버그 발생.

**원인**
게임 시작 시 실행된 `WaitForSeconds(10f)` 코루틴이 백그라운드에서 계속 실행되고 있었음.

**해결**
코루틴을 변수에 저장하고 `StopCoroutine()`을 통해 기존 타이머를 종료한 후 새로운 타이머를 시작하도록 수정.

## 📝 Known Issues

- PaddleSizeUp 아이템을 클라이언트가 획득했을 때 호스트 패들이 커지는 문제
- 스피드 아이템 획득 후 득점 시 다음 라운드에도 속도가 유지되는 문제
