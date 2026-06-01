# Baseball Batting System - 구현 체크리스트

## 섹션 0. 씬 및 환경 세팅
- [ ] BaseballScene.unity 씬 생성
- [ ] XR Device Simulator 배치 (테스트용)
- [ ] XR Origin (Complete XR Origin Set Up Variant) 배치
- [ ] 타석/마운드 위치 레이아웃 배치

## 섹션 1. 공 프리팹
- [ ] Ball 프리팹 생성 (Sphere + Rigidbody + Collider)
- [ ] Rigidbody: isKinematic=true 초기값 설정
- [ ] BaseballBall.cs 부착
- [ ] curvatureX / curvatureY AnimationCurve 조정 (변화구 궤도 튜닝)

## 섹션 2. 배트 세팅
- [ ] Bat 오브젝트 생성 (Cylinder or 커스텀 메시)
- [ ] BatController.cs 부착
- [ ] leftController / rightController Transform 연결 (XR Origin 하위 컨트롤러)
- [ ] batVisual Transform 연결
- [ ] Bat에 Collider 부착 (배트-공 충돌용)
- [ ] swingVelocityThreshold 값 튜닝

## 섹션 3. 투구 세팅
- [ ] PitchController.cs 빈 오브젝트에 부착
- [ ] pitchOrigin Transform 설정 (마운드 위치)
- [ ] strikeZoneTarget Transform 설정 (타석 스트라이크존 위치)
- [ ] Ball 프리팹 연결
- [ ] fastballSpeed / breakingBallSpeed 튜닝

## 섹션 4. 게임 매니저 세팅
- [ ] BaseballGameManager.cs 빈 오브젝트에 부착
- [ ] PitchController 참조 연결
- [ ] totalSets(3) / pitchesPerSet(10) / delayBetweenPitches 값 확인

## 섹션 5. 오디오 세팅
- [ ] 오디오 파일 3종 프로젝트에 임포트 (pitch_sound, swing_sound, hit_sound)
- [ ] PitchController AudioSource + pitchSound 클립 연결
- [ ] BatController AudioSource + swingSound / hitSound 클립 연결

## 섹션 6. 판정 테스트
- [ ] 직구 안침 → 패널티 콘솔 확인
- [ ] 직구 헛스윙 → 패널티 콘솔 확인
- [ ] 직구 타격(앞) → 성공 콘솔 확인
- [ ] 직구 타격(뒤) → 파울 콘솔 확인
- [ ] 변화구 안침 → 성공 콘솔 확인
- [ ] 변화구 헛스윙 → 패널티 콘솔 확인
- [ ] 변화구 타격(앞) → 성공 콘솔 확인
- [ ] 변화구 타격(뒤) → 파울 콘솔 확인

## 섹션 7. 게임 플로우 테스트
- [ ] 3세트 × 10구 순서 정상 진행 확인
- [ ] 세트 종료 시 점수 로그 확인
- [ ] 게임 종료 시 최종 합산 로그 확인

## 추후 확장 (UI)
- [ ] 세트 중 HUD (현재 구수 표시)
- [ ] 세트 종료 결과 화면
- [ ] 게임 종료 최종 결과 화면

---

## 스크립트 목록
| 파일 | 역할 |
|------|------|
| `BaseballGameManager.cs` | 게임 흐름 제어, 점수 집계 |
| `PitchController.cs` | 투구 생성, 공 타입 결정 |
| `BaseballBall.cs` | 공 궤도 이동, 판정 처리 |
| `BatController.cs` | 스윙 감지, 배트-공 충돌 처리 |
