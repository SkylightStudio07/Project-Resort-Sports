# [Feature] VR 양궁 미니게임 구현

리조트 스포츠 VR 체험 프로그램 내 양궁 미니게임 전체 구현 작업입니다.
플랫폼: HTC Vive Pro (PC Standalone / OpenXR + SteamVR 런타임), XR Interaction Toolkit 3.1.2
> 실기기(Vive Pro) 없이 XR Device Simulator로 개발 진행

---

## 0. 프로젝트 XR 환경 세팅 (최초 1회)

> 현재 프로젝트가 Android(Meta Quest) 기준으로 셋업되어 있어 PC 전환 필요

- [x] Unity `Build Settings` → 플랫폼을 **PC, Mac & Linux Standalone** (Windows x86_64)으로 전환 (이미 활성화됨)
- [x] `Project Settings > XR Plug-in Management` → **PC 탭**에서 **OpenXR** 활성화 (이미 설정됨)
- [x] `Project Settings > XR Plug-in Management > OpenXR` (PC 탭) 설정
  - [x] Interaction Profiles에 **HTC Vive Controller Profile** 추가 (이미 enabled)
  - [x] Render Mode: **Single Pass Instanced** (이미 설정됨)
- [x] `Window > Package Manager` → XR Interaction Toolkit → **Starter Assets + XR Device Simulator** 샘플 임포트 (이미 임포트됨)
- [ ] `XR Device Simulator` 프리팹을 씬에 배치 — 실기기 없이 키보드/마우스로 VR 시뮬레이션
  - `Assets/Samples/XR Interaction Toolkit/3.1.2/XR Device Simulator/XRDeviceSimulator/XR Device Simulator.prefab`
- [ ] Vive Pro 실기기 수령 후: PC에 **SteamVR** 설치 → SteamVR을 OpenXR 런타임으로 설정 (SteamVR 앱 내 설정)

---

## 1. 씬 & 환경 세팅

- [ ] `ArcheryScene.unity` 씬 생성
- [ ] `Complete XR Origin Set Up Variant` 프리팹 배치 (기존 VRTemplateAssets 활용)
- [ ] 사대(Shooting Line) 영역 배치 — 플레이어 시작 위치 지정
- [ ] 과녁 지지대(Target Stand) 메시 또는 프리미티브로 배치
- [ ] 거리별 과녁 위치 지정 (Easy 10m / Normal 20m / Hard 30m)
- [ ] Summer Beach 에셋으로 배경 환경 구성 (바닥, 배경 오브젝트)
- [ ] 씬 라이팅 설정 (URP)

---

## 2. 활 (Bow) 시스템

- [x] `Bow_Prefab` 생성
  - [x] 활 메시 임포트 (Free medieval weapons 에셋)
  - [x] `XRGrabInteractable` 컴포넌트 추가
  - [x] `Nocking Point` 빈 오브젝트 추가 (화살 장전 위치)
  - [x] `String Top / Bottom Point` 빈 오브젝트 추가 (stringTopnocking, stringBotnocking)
- [x] `BowController.cs` 작성
  - [x] 활 잡힘/놓임 이벤트 처리 (`selectEntered` / `selectExited`)
  - [x] 왼손 컨트롤러 위치 추적 (XRGrabInteractable 기본 동작, 카메라 고정 제거)
  - [x] 활 잡은 상태에서만 시위 인터랙션 활성화 (`isHeld` 플래그)

---

## 3. 시위 당김 (Pull Interaction) 시스템

- [x] `StringPullPoint_Prefab` 생성
  - [x] `XRGrabInteractable` 컴포넌트 추가 (오른손 전용)
  - [x] `Rigidbody` — Is Kinematic 설정
- [x] `PullInteraction.cs` 작성
  - [x] 오른손 컨트롤러 위치 ↔ `Nocking Point` 거리로 `pullAmount (0.0 ~ 1.0)` 계산
  - [x] 최대 당김 거리(`maxPullDistance`) Inspector 노출
  - [x] `pullAmount`에 따라 당김 햅틱 피드백 (세기 증가) — Vive 컨트롤러 햅틱 API 사용
  - [x] `pullAmount` 임계값(0.1 이하)에서 자동 장전 취소

---

## 4. 화살 (Arrow) 시스템

- [x] `Arrow_Prefab` 생성
  - [x] 화살 메시 임포트 (Free medieval weapons 에셋)
  - [x] `Rigidbody` 추가 (발사 전: Is Kinematic, 발사 후: 활성화)
  - [x] `CapsuleCollider` 추가
  - [ ] `TrailRenderer` 추가 (선택)
- [x] `ArrowController.cs` 작성
  - [x] 발사 시 `Rigidbody`에 `pullAmount × maxForce` 방향 힘 적용
  - [x] `Update`에서 `Rigidbody.velocity` 방향으로 화살 회전 추종
  - [x] 과녁 충돌 시 Rigidbody 비활성화 + 과녁 오브젝트의 자식으로 이동 (꽂힘 처리)
  - [x] 지면/벽 충돌 시 화살 비활성화 또는 제거
  - [x] 발사 후 5초 경과 시 자동 제거
- [x] `ArrowSpawner.cs` 작성
  - [x] 시위 당김 시작 시 `Arrow_Prefab`을 `Nocking Point`에 스폰
  - [x] 발사 또는 당김 취소 시 스폰된 화살 처리
  - [x] 남은 화살 수 차감 (`ArcheryGameManager` 연동)

---

## 5. 과녁 (Target) 시스템

- [ ] `Target_Prefab` 생성
  - [ ] 과녁 텍스처 또는 머티리얼 제작 (10점 ~ 1점 동심원)
  - [ ] 점수 존별 개별 콜라이더 10개 배치 (중심부터 바깥)
  - [ ] 각 존에 `ScoreZone` 컴포넌트 부착
- [ ] `ScoreZone.cs` 작성
  - [ ] `zoneScore` 값 Inspector 노출 (1~10)
  - [ ] `OnTriggerEnter`에서 `ArrowController` 태그 확인 후 점수 이벤트 발행
  - [ ] 같은 화살이 여러 존 트리거를 통과할 때 최초 1회만 점수 처리

---

## 6. 게임 매니저 (ArcheryGameManager)

- [ ] `ArcheryGameManager.cs` 작성
  - [ ] 게임 상태 열거형: `Idle / Loading / Playing / RoundEnd / GameOver`
  - [ ] 라운드당 화살 수 설정 (기본 3발)
  - [ ] 총 라운드 수 설정 (기본 3라운드)
  - [ ] 점수 누적 및 라운드별 점수 기록
  - [ ] 화살 소진 시 라운드 종료 처리
  - [ ] 전 라운드 종료 시 `GameOver` 전환 + 최종 점수 집계
  - [ ] 재시작 메서드 구현

---

## 7. UI

- [ ] `ScoreBoard` — World Space Canvas 제작
  - [ ] 현재 라운드 표시 (예: Round 2 / 3)
  - [ ] 이번 라운드 점수 표시
  - [ ] 남은 화살 수 아이콘 표시
  - [ ] 총 누적 점수 표시
- [ ] 발사 직후 점수 팝업 (예: "+10!" 텍스트 — 빌보드 방식)
- [ ] 게임 종료 결과 패널
  - [ ] 라운드별 점수 목록
  - [ ] 최종 점수 및 등급 (예: S / A / B / C)
  - [ ] 재시작 / 메인 메뉴 버튼
- [ ] `ArcheryUI.cs` 작성 — `ArcheryGameManager` 이벤트 구독하여 UI 갱신

---

## 8. 오디오

- [x] 시위 당기는 소리 — `PullInteraction`에서 루프 재생, 취소 시 정지 (bow_loading 클립 연결 완료)
- [x] 발사음 — 오른손 놓을 때 `PlayOneShot` (클립 연결 완료)
- [ ] 과녁 명중음 (점수 존별 다른 소리 또는 파티클)
- [ ] 미스 소리 (땅/벽 명중)
- [x] `AudioSource` 설정 — `StringPullPoint`에 컴포넌트 추가 및 연결 완료

---

## 9. 프리팹 & 폴더 구조 정리

- [ ] `Assets/Archery/` 폴더 생성
  - [ ] `Scripts/` — 위 스크립트 전체
  - [ ] `Prefabs/` — Bow, Arrow, StringPullPoint, Target
  - [ ] `Materials/` — 과녁 머티리얼, 활/화살 머티리얼
  - [ ] `Audio/` — 발사음, 명중음 등
  - [ ] `Textures/` — 과녁 텍스처

---

## 10. 테스트 & QA

### 시뮬레이터 테스트 (실기기 없이)
- [ ] XR Device Simulator로 활 잡기 / 시위 당김 / 발사 기능 테스트
  - 마우스 우클릭: 그립 시뮬레이션 / WASD: 이동 / G키: 좌우 컨트롤러 전환
- [ ] 발사 물리 튜닝 (maxForce, 중력 스케일) — 시뮬레이터 기준 1차 조정
- [ ] 점수 판정 오작동 여부 확인
- [ ] UI 가독성 및 월드 스페이스 위치 확인

### 실기기 테스트 (Vive Pro 수령 후)
- [ ] SteamVR → OpenXR 런타임 설정 확인 후 빌드 실행
- [ ] 활 그립 편의성 확인 (Attach Transform 조정, Vive 컨트롤러 그립 버튼 기준)
- [ ] 발사 물리 실환경 재튜닝 (시뮬레이터와 체감 차이 보정)
- [ ] 과녁 거리별 난이도 체감 검증 (10m / 20m / 30m)
- [ ] 화살 꽂힘 연출 확인
- [ ] 오디오 볼륨 및 타이밍 확인
- [ ] 햅틱 피드백 강도 조정

---

**담당:** hyojoon Lim
**관련 씬:** `Assets/Scenes/ArcheryScene.unity`
**참고:** XR Interaction Toolkit 3.1.2, OpenXR 1.14.3, URP 14.0.12, SteamVR (Vive Pro 런타임)
