using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public sealed partial class 신들의전쟁
{
    VisualElement 병과선택막대, 훈련비용상자, 훈련비용합계;
    Label 병과선택제목, 훈련비용제목, 훈련대기표시, 훈련비용설명;
    readonly Label[] 훈련재화수량 = new Label[채집아이콘이름.Length];
    Button 주민선택버튼, 일반선택버튼, 브루저선택버튼, 아처선택버튼, 선택복원버튼;
    readonly HashSet<int> 필터원본 = new(), 필터결과 = new();
    readonly (int 병사, int 군단)[] 군단구성 = new (int, int)[전장규칙.전체칸];
    const int 군단수 = 4;
    readonly Button[] 군단버튼들 = new Button[군단수];
    readonly Action[] 군단편입동작 = new Action[군단수];
    readonly Texture2D[] 군단휘장들 = new Texture2D[군단수];
    readonly int[] 군단인원 = new int[군단수];
    readonly Vector2[] 군단중심 = new Vector2[군단수];
    VisualElement 군단막대;

    void 군단버튼준비()
    {
        군단막대 = 뿌리.Q("army-corps");
        군단막대.style.unityFont = 도움.font.sourceFontFile;
        for (int i = 0; i < 군단버튼들.Length; i++)
        {
            int 군단 = i + 1;
            var 버튼 = 군단버튼들[i] = 군단막대.Q<Button>($"corps-{군단}");
            군단휘장들[i] = Resources.Load<Texture2D>($"Minimap/corps-{군단}");
            버튼.Q(className: "corps-emblem").style.backgroundImage = new StyleBackground(군단휘장들[i]);
            군단편입동작[i] = () =>
            {
                군단편입(군단);
                버튼.Blur();
            };
            버튼.clicked += 군단편입동작[i];
        }
    }

    void 군단버튼해제()
    {
        if (군단막대 == null) return;
        for (int i = 0; i < 군단버튼들.Length; i++)
            군단버튼들[i].clicked -= 군단편입동작[i];
        표시(군단막대, false);
    }

    void 군단편입(int 군단)
    {
        if (!선택필터사용가능 || 배치 >= 0) return;
        bool 편입함 = false;
        foreach (int 번호 in 선택부대)
        {
            if (!내병력인가(번호) || 상태.대상들[번호].병사번호 <= 0) continue;
            군단구성[번호] = (상태.대상들[번호].병사번호, 군단);
            편입함 = true;
        }
        if (편입함) 선택효과음재생();
    }

    void 군단구성정리(전장상태 현재)
    {
        for (int i = 0; i < 군단구성.Length; i++)
        {
            var 소속 = 군단구성[i];
            if (소속.군단 == 0) continue;
            var 대상 = 현재.대상들[i];
            if (대상 is { 생존: true, 종류: 대상종류.병사 } && 대상.병사번호 == 소속.병사) continue;
            군단구성[i] = default;
            선택부대.Remove(i);
        }
    }

    void 군단선택(int 군단)
    {
        if (!선택필터사용가능) return;
        군단구성정리(상태);
        선택부대.Clear();
        for (int i = 0; i < 군단구성.Length; i++)
            if (군단구성[i].군단 == 군단 && 내병력인가(i)) 선택부대.Add(i);
        필터원본.Clear();
        필터결과.Clear();
        드래그선택안내 = false;
        드래그중 = false;
        배치 = -1;
        건축목록표시(false);
        if (선택부대.Count > 0) 선택효과음재생();
    }

    void 군단막대갱신()
    {
        군단구성정리(상태);
        군단통계갱신();
        bool 보임 = 선택필터사용가능 && 배치 < 0 && !드래그중;
        if (보임)
        {
            보임 = false;
            foreach (int 번호 in 선택부대)
                if (내병력인가(번호)) { 보임 = true; break; }
        }
        표시(군단막대, 보임);
        if (!보임) return;

        var 왕패널 = (RectTransform)왕상태.rectTransform.parent.parent;
        var 왼쪽위 = 왕패널.TransformPoint(new Vector3(왕패널.rect.xMin, 왕패널.rect.yMax));
        var 기준 = 화면변환(RectTransformUtility.WorldToScreenPoint(null, 왼쪽위));
        군단막대.style.left = 기준.x;
        군단막대.style.bottom = 뿌리.contentRect.height - 기준.y + 6f;

        for (int i = 0; i < 군단버튼들.Length; i++)
            군단버튼들[i].text = 군단휘장들[i] != null ? $"군단 · {군단인원[i]}" : $"제 {i + 1}군단 · {군단인원[i]}";
    }

    void 군단통계갱신()
    {
        Array.Clear(군단인원, 0, 군단수);
        Array.Clear(군단중심, 0, 군단수);
        for (int i = 0; i < 상태.대상들.Length; i++)
        {
            var 소속 = 군단구성[i];
            int 번호 = 소속.군단 - 1;
            if ((uint)번호 >= 군단수 || !내병력인가(i) || 상태.대상들[i].병사번호 != 소속.병사) continue;
            군단인원[번호]++;
            군단중심[번호] += 상태.대상들[i].위치;
        }
        for (int i = 0; i < 군단수; i++)
            if (군단인원[i] > 0) 군단중심[i] /= 군단인원[i];
    }

    void 선택훈련안내준비()
    {
        병과선택막대 = 뿌리.Q("army-class-filter");
        병과선택제목 = 뿌리.Q<Label>("army-class-title");
        주민선택버튼 = 뿌리.Q<Button>("select-resident");
        일반선택버튼 = 뿌리.Q<Button>("select-regular");
        브루저선택버튼 = 뿌리.Q<Button>("select-bruiser");
        아처선택버튼 = 뿌리.Q<Button>("select-archer");
        선택복원버튼 = 뿌리.Q<Button>("restore-selection");
        훈련비용상자 = 뿌리.Q("training-cost");
        훈련비용제목 = 뿌리.Q<Label>("training-cost-title");
        훈련대기표시 = 뿌리.Q<Label>("training-queue");
        훈련비용합계 = 뿌리.Q("training-cost-total");
        훈련비용설명 = 뿌리.Q<Label>("training-cost-detail");
        for (int i = 0; i < 훈련재화수량.Length; i++)
        {
            뿌리.Q($"training-icon-{i}").style.backgroundImage = new StyleBackground(채집아이콘들[i]);
            훈련재화수량[i] = 뿌리.Q<Label>($"training-amount-{i}");
        }
        병과선택막대.style.unityFont = 도움.font.sourceFontFile;
        훈련비용상자.style.unityFont = 도움.font.sourceFontFile;
        주민선택버튼.clicked += 주민만선택;
        일반선택버튼.clicked += 일반만선택;
        브루저선택버튼.clicked += 브루저만선택;
        아처선택버튼.clicked += 아처만선택;
        선택복원버튼.clicked += 선택복원;
        필터원본.Clear(); 필터결과.Clear();
    }

    void 선택훈련안내해제()
    {
        if (주민선택버튼 != null) 주민선택버튼.clicked -= 주민만선택;
        if (일반선택버튼 != null) 일반선택버튼.clicked -= 일반만선택;
        if (브루저선택버튼 != null) 브루저선택버튼.clicked -= 브루저만선택;
        if (아처선택버튼 != null) 아처선택버튼.clicked -= 아처만선택;
        if (선택복원버튼 != null) 선택복원버튼.clicked -= 선택복원;
        if (병과선택막대 != null) 표시(병과선택막대, false);
        if (훈련비용상자 != null) 표시(훈련비용상자, false);
        필터원본.Clear(); 필터결과.Clear();
    }

    bool 선택필터사용가능 => 전투중 && 상태 != null && 상태.승자 < 0 && !패널열림 && !일시정지 && 주민패널?.열림 != true;
    void 주민만선택() => 병과필터(null);
    void 일반만선택() => 병과필터(병과.일반);
    void 브루저만선택() => 병과필터(병과.브루저);
    void 아처만선택() => 병과필터(병과.아처);

    bool 부류선택가능(전장대상 대상, 병과? 종류) => 종류.HasValue
        ? 전장규칙.병과선택가능(대상, 내팀, 종류.Value) : 전장규칙.주민선택가능(대상, 내팀);

    void 병과필터(병과? 종류, bool 제외 = false)
    {
        if (!선택필터사용가능) return;
        if (필터원본.Count == 0 || !선택부대.SetEquals(필터결과))
        { 필터원본.Clear(); 필터원본.UnionWith(선택부대); }
        bool 일치(int 번호) => (uint)번호 < (uint)상태.대상들.Length && 부류선택가능(상태.대상들[번호], 종류);
        if (제외) 선택부대.RemoveWhere(일치);
        else
        {
            선택부대.Clear();
            foreach (int 번호 in 필터원본)
                if (일치(번호)) 선택부대.Add(번호);
        }
        필터결과.Clear(); 필터결과.UnionWith(선택부대);
        선택효과음재생();
        드래그선택안내 = false; 드래그중 = false; 배치 = -1;
        건축목록표시(false);
    }

    void 선택복원()
    {
        if (!선택필터사용가능) return;
        선택부대.Clear();
        foreach (int 번호 in 필터원본)
            if ((uint)번호 < (uint)상태.대상들.Length && 상태.대상들[번호] is { 생존: true } 대상
                && 대상.팀 == 내팀 && 대상.종류 is 대상종류.주민 or 대상종류.병사) 선택부대.Add(번호);
        선택효과음재생();
        필터원본.Clear(); 필터결과.Clear();
    }

    bool 병과선택단축키(Keyboard 키)
    {
        if (배치 >= 0) return false;
        병과? 종류;
        if (키.zKey.wasPressedThisFrame) 종류 = null;
        else if (키.xKey.wasPressedThisFrame) 종류 = 병과.일반;
        else if (키.cKey.wasPressedThisFrame) 종류 = 병과.브루저;
        else if (키.vKey.wasPressedThisFrame) 종류 = 병과.아처;
        else return false;
        병과필터(종류, 키.shiftKey.isPressed);
        return true;
    }

    void 선택훈련안내갱신()
    {
        if (병과선택막대 == null) return;
        // A new click, drag or Tab begins a new selection group.
        if (필터원본.Count > 0 && !선택부대.SetEquals(필터결과)) { 필터원본.Clear(); 필터결과.Clear(); }
        bool 보임 = 선택필터사용가능 && 배치 < 0 && !드래그중 && (선택부대.Count > 0 || 필터원본.Count > 0);
        표시(병과선택막대, 보임);
        if (보임)
        {
            var 후보 = 필터원본.Count > 0 ? 필터원본 : 선택부대;
            int 수(병과? 종류) { int 수 = 0; foreach (int i in 후보) if ((uint)i < (uint)상태.대상들.Length && 부류선택가능(상태.대상들[i], 종류)) 수++; return 수; }
            int 주민 = 수(null), 일반 = 수(병과.일반), 브루저 = 수(병과.브루저), 아처 = 수(병과.아처);
            병과선택제목.text = $"선택한 인간 {후보.Count}명 중 · 현재 {선택부대.Count}명";
            주민선택버튼.text = $"주민 {주민}  [Z]"; 주민선택버튼.SetEnabled(주민 > 0);
            일반선택버튼.text = $"병사 {일반}  [X]"; 일반선택버튼.SetEnabled(일반 > 0);
            브루저선택버튼.text = $"브루저 {브루저}  [C]"; 브루저선택버튼.SetEnabled(브루저 > 0);
            아처선택버튼.text = $"아처 {아처}  [V]"; 아처선택버튼.SetEnabled(아처 > 0);
            선택복원버튼.SetEnabled(필터원본.Count > 0);
        }
        훈련비용안내갱신();
    }

    void 훈련비용안내갱신()
    {
        int 번호 = 선택필터사용가능 && 배치 < 0 && Mouse.current != null && Application.isFocused && 전장가리킴(현재마우스)
            ? 클릭대상(전장좌표(현재마우스), 클릭여유, 아군훈련소만: true) : -1;
        if (번호 >= 0 && 상태.대상들[번호].완공대기 <= 0) 훈련비용안내표시(번호);
        else 표시(훈련비용상자, false);
    }

    void 훈련비용안내표시(int 번호)
    {
        var 건물 = 상태.대상들[번호];
        if (건물.팀 != 내팀 || (건물.종류 != 대상종류.훈련소 && !전장규칙.전문훈련소(건물))) return;
        bool 전문 = 전장규칙.전문훈련소(건물);
        int 인원 = 0;
        foreach (int i in 선택부대)
            if ((uint)i < (uint)상태.대상들.Length && 전장규칙.훈련가능유닛(내팀, 건물, 상태.대상들[i])) 인원++;
        int 병력 = 전문 ? 0 : 전장규칙.양성예정병력(상태, 내팀);
        int 선택인원 = 인원;
        if (!전문) 인원 = Mathf.Min(인원, Mathf.Max(0, 전장규칙.병력제한 - 병력));
        string 이름 = 건물.종류 == 대상종류.훈련소 ? "병사 훈련소" : 건물.종류 == 대상종류.브루저훈련소 ? "브루저 훈련소" : "아처 훈련소";
        bool 비용표시 = 인원 > 0;
        훈련비용제목.text = 비용표시 ? $"{이름} · {인원}명 비용" : 이름;
        훈련대기표시.text = 건물.훈련대기수 == 0 ? "대기 0명"
            : $"대기열 {건물.훈련대기수}명 · {Mathf.CeilToInt(건물.훈련잔여합계)}초 (이동 제외)";
        표시(훈련비용합계, 비용표시);
        if (비용표시)
        {
            var 재화 = 상태.진영들[내팀].재화;
            for (int i = 0; i < 훈련재화수량.Length; i++)
            {
                int 필요 = 0;
                for (int j = 0; j < 인원; j++)
                    필요 += 전장규칙.병사훈련비용(i, 전문 ? 0 : 병력 + j);
                var 수량 = 훈련재화수량[i];
                수량.text = 필요.ToString();
                수량.style.color = 재화[i] >= 필요 ? new Color(.94f, .84f, .63f) : new Color(1, .48f, .42f);
                표시(수량.parent, 필요 > 0);
            }
        }
        훈련비용설명.text = 선택인원 > 0 && 인원 == 0 ? "병력 정원 가득 참" : "";
        표시(훈련비용설명, 훈련비용설명.text.Length > 0);
        Vector2 점 = 위치변환(건물.위치 + Vector2.up * 2.1f);
        var 영역 = 뿌리.contentRect;
        float 너비 = Mathf.Min(300, 영역.width * .96f);
        훈련비용상자.style.left = Mathf.Clamp(점.x - 너비 / 2, 8, Mathf.Max(8, 영역.width - 너비 - 8));
        훈련비용상자.style.bottom = Mathf.Max(8, 영역.height - Mathf.Max(76, 점.y - 12));
        표시(훈련비용상자, true);
    }
}
