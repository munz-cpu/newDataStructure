using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed partial class 신들의전쟁
{
    enum 디버그위치도구 { 없음, 순간이동, 허수아비배치, 허수아비제거 }
    const float 재료기본여백 = 13;

    VisualElement 디버그패널;
    Button 디버그열기버튼, 디버그숨기기버튼;
    bool 디버그패널접힘;
    Label 디버그안내;
    Button 디버그무적버튼, 디버그AI버튼, 디버그훈련버튼, 디버그조종버튼, 디버그자동플레이버튼;
    DropdownField 디버그직업, 디버그스킬칸, 디버그스킬;
    Sprite 허수아비그림;
    디버그위치도구 디버그도구;
    int 원래팀;

    bool 디버그사용가능 => 규칙?.디버그연습전 == true && 전투중 && 상태.승자 < 0
        && !멀티 && !패널열림 && !일시정지 && !주민패널.열림;

    void 디버그준비()
    {
        if (규칙?.디버그연습전 != true) return;
        원래팀 = 내팀;
        디버그도구 = 디버그위치도구.없음;
        허수아비그림 = Resources.Load<Sprite>("practice-target");
        디버그패널 = 뿌리.Q("practice-debug");
        디버그패널접힘 = false;
        디버그열기버튼 = 뿌리.Q<Button>("debug-show-panel");
        디버그숨기기버튼 = 디버그패널.Q<Button>("debug-hide-panel");
        디버그열기버튼.style.unityFont = 도움.font.sourceFontFile;
        디버그열기버튼.clicked += 디버그패널전환;
        디버그숨기기버튼.clicked += 디버그패널전환;
        디버그패널.style.unityFont = 도움.font.sourceFontFile;
        디버그안내 = 디버그패널.Q<Label>("debug-status");
        임무카드.RegisterCallback<GeometryChangedEvent>(디버그패널위치갱신);
        뿌리.RegisterCallback<GeometryChangedEvent>(디버그패널위치갱신);

        디버그버튼("debug-teleport", () => 디버그도구선택(디버그위치도구.순간이동, "미니맵에서 이동할 곳을 클릭하세요."));
        디버그버튼("debug-heal", () =>
        {
            규칙.디버그완전회복(내팀);
            디버그안내.text = "왕의 체력을 모두 회복했습니다.";
        });
        디버그무적버튼 = 디버그버튼("debug-invulnerability", () => 규칙.디버그무적전환(내팀));
        디버그AI버튼 = 디버그버튼("debug-ai-pause", () => 규칙.디버그AI정지 = !규칙.디버그AI정지);
        디버그자동플레이버튼 = 디버그버튼("debug-autoplay", 디버그자동플레이전환);

        var 재화 = 디버그패널.Q<DropdownField>("debug-resource");
        재화.choices = new List<string>(전장규칙.재화이름);
        재화.index = 0;
        디버그버튼("debug-grant", () =>
        {
            if (재화.index < 0) return;
            규칙.디버그재화지급(내팀, 재화.index);
            디버그안내.text = $"{재화.value} 1,000개를 지급했습니다.";
        });

        디버그훈련버튼 = 디버그버튼("debug-training", () => 규칙.디버그즉시훈련전환(내팀));
        디버그버튼("debug-cooldowns", () =>
        {
            규칙.디버그쿨다운초기화(내팀);
            디버그안내.text = "스킬 쿨타임을 모두 초기화했습니다.";
        });

        디버그버튼("debug-place-dummy", () => 디버그도구선택(디버그위치도구.허수아비배치, "전장에서 허수아비를 놓을 곳을 클릭하세요."));
        디버그버튼("debug-remove-dummy", () => 디버그도구선택(디버그위치도구.허수아비제거, "제거할 허수아비를 클릭하세요."));
        디버그조종버튼 = 디버그버튼("debug-takeover", 디버그조종전환);

        디버그직업 = 디버그패널.Q<DropdownField>("debug-class");
        디버그직업.choices = new List<string>(Array.ConvertAll(왕직업목록.선택, 직업 => 직업.ToString()));
        디버그버튼("debug-change-class", () =>
        {
            if (디버그직업.index < 0 || !규칙.디버그직업변경(내팀, 왕직업목록.선택[디버그직업.index])) return;
            디버그입력초기화();
            왕그림들[내팀] = default;
            디버그직업선택갱신();
            디버그안내.text = $"직업을 {디버그직업.value}(으)로 바꿨습니다.";
        });
        디버그스킬칸 = 디버그패널.Q<DropdownField>("debug-skill-slot");
        디버그스킬키갱신();
        디버그스킬 = 디버그패널.Q<DropdownField>("debug-skill");
        디버그스킬칸.RegisterValueChangedCallback(디버그스킬칸변경);
        디버그버튼("debug-change-skill", () =>
        {
            if (!규칙.디버그스킬변경(내팀, 디버그스킬칸.index, 디버그스킬.index)) return;
            스킬조준취소();
            디버그안내.text = $"{디버그스킬칸.value} 슬롯에 {디버그스킬.value} 장착";
        });

        디버그직업선택갱신();
        디버그버튼갱신();
    }

    Button 디버그버튼(string 이름, Action 동작)
    {
        var 버튼 = 디버그패널.Q<Button>(이름);
        버튼.clicked += () =>
        {
            if (!디버그사용가능) return;
            동작();
            디버그버튼갱신();
        };
        return 버튼;
    }

    void 디버그패널위치갱신(GeometryChangedEvent 입력)
    {
        if (디버그패널 != null && 임무카드.layout.height > 0)
            디버그패널.style.top = 디버그열기버튼.style.top = 임무카드.layout.yMax + 8;
    }

    void 디버그패널전환()
    {
        if (!디버그사용가능) return;
        디버그패널접힘 = !디버그패널접힘;
        if (디버그패널접힘) 디버그도구취소();
        디버그패널표시갱신();
    }

    void 디버그패널표시갱신()
    {
        if (디버그패널 == null) return;
        bool 표시가능 = 디버그사용가능;
        표시(디버그패널, 표시가능 && !디버그패널접힘);
        표시(디버그열기버튼, 표시가능 && 디버그패널접힘);
    }

    void 디버그해제()
    {
        if (디버그패널 == null) return;
        표시(디버그패널, false);
        표시(디버그열기버튼, false);
        디버그열기버튼.clicked -= 디버그패널전환;
        디버그숨기기버튼.clicked -= 디버그패널전환;
        임무카드.UnregisterCallback<GeometryChangedEvent>(디버그패널위치갱신);
        뿌리.UnregisterCallback<GeometryChangedEvent>(디버그패널위치갱신);
        디버그스킬칸.UnregisterValueChangedCallback(디버그스킬칸변경);
        디버그패널 = null;
        디버그스킬칸 = null;
        재료가로위치(재료기본여백);
    }

    void 디버그재료위치갱신()
    {
        if (디버그패널 == null) return;
        if (!디버그사용가능 || 디버그패널접힘 || 디버그패널.layout.width <= 0)
        {
            재료가로위치(재료기본여백);
            return;
        }
        var 부모 = (RectTransform)전투.transform;
        var 오른쪽 = 화면좌표(new Vector2(디버그패널.worldBound.xMax, 0));
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(부모, 오른쪽, null, out var 점))
            재료가로위치(점.x - 부모.rect.xMin + 재료기본여백);
    }

    void 재료가로위치(float 위치)
    {
        foreach (var 글 in 재료수량글)
        {
            var 행 = (RectTransform)글.rectTransform.parent;
            var 좌표 = 행.anchoredPosition;
            if (Mathf.Approximately(좌표.x, 위치)) continue;
            좌표.x = 위치;
            행.anchoredPosition = 좌표;
        }
    }

    void 디버그스킬키갱신()
    {
        if (디버그스킬칸 == null) return;
        int 칸 = Mathf.Max(0, 디버그스킬칸.index);
        디버그스킬칸.choices = new List<string> { "Q", 키보드이동 ? "F" : "W", "E", "R" };
        디버그스킬칸.SetValueWithoutNotify(디버그스킬칸.choices[칸]);
    }

    void 디버그버튼갱신()
    {
        디버그무적버튼.text = 규칙.디버그무적(내팀) ? "무적 · 켜짐" : "무적 · 꺼짐";
        디버그AI버튼.text = 규칙.디버그AI정지 ? "적색 AI · 정지" : "적색 AI · 작동";
        디버그AI버튼.SetEnabled(!규칙.디버그AI수동조종);
        if (규칙.디버그AI수동조종) 디버그AI버튼.text = "적색 AI · 수동 조종 중";
        디버그훈련버튼.text = 규칙.디버그즉시훈련(내팀) ? "훈련 0초 · 켜짐" : "훈련 0초 · 꺼짐";
        디버그조종버튼.text = 규칙.디버그AI수동조종 ? "청록 진영으로 복귀" : "적색 AI 기지 조종";
        디버그조종버튼.SetEnabled(!규칙.디버그자동플레이);
        디버그자동플레이버튼.text = 규칙.디버그자동플레이 ? "자동 플레이 끄기" : "자동 플레이 켜기";
        디버그자동플레이버튼.SetEnabled(!규칙.디버그AI수동조종);
    }

    void 디버그직업선택갱신()
    {
        var 진영 = 상태.진영들[내팀];
        디버그직업.SetValueWithoutNotify(진영.직업.ToString());
        var 후보이름 = new List<string>(왕직업목록.장착후보수(진영.직업));
        for (int i = 0; i < 왕직업목록.장착후보수(진영.직업); i++)
            후보이름.Add(왕직업목록.정보(왕직업목록.후보(진영.직업, i)).이름);
        디버그스킬.choices = 후보이름;
        디버그장착선택갱신();
    }

    void 디버그스킬칸변경(ChangeEvent<string> 입력) => 디버그장착선택갱신();

    void 디버그장착선택갱신()
    {
        int 칸 = 디버그스킬칸.index;
        if (칸 < 0 || 디버그스킬.choices.Count == 0) return;
        int 장착 = 왕직업목록.후보번호(상태.진영들[내팀].장착구성, 칸);
        디버그스킬.SetValueWithoutNotify(디버그스킬.choices[장착]);
    }

    void 디버그입력초기화()
    {
        디버그화면입력초기화();
        규칙.디버그왕정지(내팀);
    }

    void 디버그화면입력초기화()
    {
        지도누름해제();
        스킬조준취소();
        보낸이동방향 = 채집전이동방향 = Vector2.zero;
        보낸달리기 = false;
        드래그중 = false;
        배치 = -1;
        건축목록표시(false);
        망치커서표시(false);
    }

    void 디버그자동플레이전환()
    {
        if (!규칙.디버그자동플레이전환()) return;
        디버그화면입력초기화();
        디버그도구취소();
        선택부대.Clear();
        필터원본.Clear();
        필터결과.Clear();
        디버그안내.text = 규칙.디버그자동플레이 ? "AI가 청록 진영을 조종 중입니다." : "청록 진영을 직접 조종합니다.";
    }

    void 디버그조종전환()
    {
        디버그입력초기화();
        디버그도구취소();
        선택부대.Clear();
        필터원본.Clear();
        필터결과.Clear();
        주민스탯번호 = -1;
        상태창?.숨기기();
        식량창.닫기();
        규칙.디버그AI수동조종 = !규칙.디버그AI수동조종;
        내팀 = 규칙.디버그AI수동조종 ? 1 - 원래팀 : 원래팀;
        규칙.디버그왕정지(내팀);
        표시한임무 = 표시한알림번호 = 표시한모집초 = -1;
        마지막적중순번 = 상태.진영들[내팀].적중순번;
        시점복귀();
        전장카메라.transform.position = 카메라한계(내왕().위치);
        디버그직업선택갱신();
        디버그안내.text = 규칙.디버그AI수동조종 ? "적색 진영 조종 중" : "청록 진영으로 복귀했습니다.";
    }

    void 디버그도구선택(디버그위치도구 도구, string 안내)
    {
        if (디버그도구 == 도구)
        {
            디버그도구취소();
            return;
        }
        디버그입력초기화();
        디버그도구 = 도구;
        디버그안내.text = 안내;
    }

    bool 디버그도구취소()
    {
        if (디버그도구 == 디버그위치도구.없음) return false;
        디버그도구 = 디버그위치도구.없음;
        디버그안내.text = "위치 지정을 취소했습니다.";
        return true;
    }

    bool 디버그지도클릭(Vector2 위치)
    {
        if (!디버그사용가능 || 디버그도구 != 디버그위치도구.순간이동) return false;
        if (!규칙.디버그순간이동(내팀, 위치))
        {
            디버그안내.text = "왕이 살아 있을 때 통행 가능한 땅을 클릭하세요.";
            return true;
        }
        디버그도구 = 디버그위치도구.없음;
        시점복귀();
        전장카메라.transform.position = 카메라한계(내왕().위치);
        디버그안내.text = "왕이 순간이동했습니다.";
        return true;
    }

    bool 디버그전장클릭(Vector2 위치)
    {
        if (!디버그사용가능) return false;
        switch (디버그도구)
        {
            case 디버그위치도구.허수아비배치:
                if (규칙.디버그허수아비배치(내팀, 위치))
                {
                    디버그도구 = 디버그위치도구.없음;
                    디버그안내.text = "허수아비를 소환했습니다.";
                }
                else 디버그안내.text = $"다른 빈 땅을 클릭하세요. 최대 {전장규칙.디버그허수아비한도}개까지 소환할 수 있습니다.";
                return true;
            case 디버그위치도구.허수아비제거:
                int 번호 = 클릭대상(위치, 클릭여유);
                if (번호 >= 0 && 상태.대상들[번호] is { 연습허수아비: true } 허수아비) 위치 = 허수아비.위치;
                if (규칙.디버그허수아비제거(내팀, 위치))
                {
                    디버그도구 = 디버그위치도구.없음;
                    디버그안내.text = "허수아비를 제거했습니다.";
                }
                else 디버그안내.text = "제거할 허수아비를 클릭하세요.";
                return true;
            default: return false;
        }
    }
}
