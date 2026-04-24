# Wandering Caravan Event Summary

> **태그**: WanderingCaravan Event Flow Naming Def Structure

---

## 1. Def 파일 구성 (WanderingTrader 폴더)

| 파일 | Def 타입 | defName |
|------|----------|---------|
| Incident_WanderingTrader.xml | IncidentDef | RK_Incident_WanderingTrader |
| TraderKind_WanderingTrader.xml | TraderKindDef | RK_TraderKind_WanderingTrader |
| PawnKinds_WanderingTrader.xml | PawnKindDef | RK_PawnKind_Nomad, RK_PawnKind_Wanderer, RK_PawnKind_CaravanLeader, RK_PawnKind_CaravanGuard |
| Backstory_Caravan_Base.xml | AlienBackstoryDef | RK_Backstory_CaravanBase, RK_Backstory_WandererChildhoodBase, RK_Backstory_WandererAdulthoodBase, RK_Backstory_CaravanLeaderBase |
| Backstory_Caravan_Child.xml | AlienBackstoryDef | RK_Backstory_WandererChild |
| Backstory_Caravan_Adult.xml | AlienBackstoryDef | RK_Backstory_Gypsy, RK_Backstory_RefugeeSlave, RK_Backstory_WanderingRefugee, RK_Backstory_CaravanLeader, RK_Backstory_WanderingMercenary |

---

## 2. 외부 참조 Def (WanderingTrader 외부)

| 위치 | Def | 용도 |
|------|-----|------|
| FactionDefs/Factions_Misc.xml | RK_Faction_Caravan | 캐러반 팩션 (hidden) |
| JobDefs/jobdef.xml | RK_Job_TalkToCaravanLeader | 리더 대화 Job |
| Storytellers/Storytellers.xml | StorytellerCompProperties_WanderingCaravan | 스토리텔러 연동 |

---

## 3. 이벤트 플로우

```
[StorytellerComp_WanderingCaravan]
  └─ minDaysPassed(30) 이후 MTB 체크 (봄 3일, 여름 1.5일, 기타 0.5일)
  └─ minRefireDays(45) 간격
  └─ RK_Incident_WanderingTrader 발동

[IncidentWorker_RatkinWanderingTrader]
  └─ RK_Faction_Caravan 확보/생성
  └─ Pawn 생성: 리더(1) + 호위(2~4) + 유랑민(salePawnCountRange: 1~2)
  └─ LordJob_WanderingCaravan 부여

[LordJob_WanderingCaravan]
  Travel → Idle → Exit
  ├─ Travel: chillSpot 이동 → TravelArrived
  ├─ Idle: 리더 우클릭 → ExtraFloatMenuOptions (대화하기, 돌려보내기)
  │   ├─ 대화하기 → JobDriver_TalkToCaravanLeader → Dialog_NodeTree
  │   │   ├─ 물자 거래 → Dialog_Trade
  │   │   ├─ 물건 의뢰 → Dialog_CaravanCommission (플레이스홀더)
  │   │   ├─ 합류 제안 → Dialog_CaravanSettlers
  │   │   └─ 대화 마치기
  │   └─ 돌려보내기 → Memo "CaravanDismissed"
  ├─ Idle → Exit 트리거:
  │   ├─ TicksPassed(27000~45000)
  │   ├─ CaravanDismissed
  │   ├─ PawnExperiencingDangerousTemperatures
  │   ├─ BecamePlayerEnemy → ExitMapAndDefendSelf
  │   └─ PawnHarmed → ExitMapAndDefendSelf
  └─ ExitMap / ExitMapAndDefendSelf
```

---

## 4. 네이밍 규칙

| 구분 | 접두사/패턴 | 예시 |
|------|-------------|------|
| Incident | RK_Incident_ | RK_Incident_WanderingTrader |
| TraderKind | RK_TraderKind_ | RK_TraderKind_WanderingTrader |
| PawnKind | RK_PawnKind_ | RK_PawnKind_CaravanLeader, RK_PawnKind_CaravanGuard, RK_PawnKind_Nomad, RK_PawnKind_Wanderer |
| Backstory | RK_Backstory_ | RK_Backstory_CaravanLeader, RK_Backstory_WandererChild |
| Faction | RK_Faction_ | RK_Faction_Caravan |
| Job | RK_Job_ | RK_Job_TalkToCaravanLeader |
| 언어 키 (캐러반) | RK_WanderingCaravan_ | RK_WanderingCaravan_TalkToLeader, RK_WanderingCaravan_ArrivalLetter |
| 언어 키 (구버전) | RK_WanderingTrader_ | RK_WanderingTrader_Info, RK_WanderingTrader_ViewSettlers |

---

## 5. C# 클래스 매핑

| 클래스 | 역할 |
|--------|------|
| IncidentWorker_RatkinWanderingTrader | 인시던트 실행, Pawn/Lord 생성 |
| IncidentDefExtension_WanderingCaravan | salePawnCountRange XML 확장 |
| LordJob_WanderingCaravan | Travel→Idle→Exit 상태 그래프 |
| LordToil_WanderingCaravanIdle | 대기 + ExtraFloatMenuOptions + CreateMainDialog |
| JobDriver_TalkToCaravanLeader | 리더에게 이동 후 대화창 오픈 |
| Dialog_CaravanCommission | 물건 의뢰 (플레이스홀더) |
| Dialog_CaravanSettlers | 유랑민 합류 제안 |
| StorytellerComp_WanderingCaravan | 시즌별 MTB 이벤트 트리거 |
| StorytellerCompProperties_WanderingCaravan | springMtbDays, summerMtbDays, fallbackMtbDays, minRefireDays |
| FloatMenuOptionProvider_Trade_Patch | LordJob_WanderingCaravan 리더일 때 거래 FloatMenu 대체 |
