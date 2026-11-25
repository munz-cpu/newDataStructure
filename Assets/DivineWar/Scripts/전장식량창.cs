using System;
using UnityEngine;
using UnityEngine.UIElements;

sealed class 전장식량창 : IDisposable
{
    readonly Button 버튼;
    readonly VisualElement 패널;
    readonly Label 상태글;
    readonly Label[] 수량들 = new Label[전장식량.종류수];
    readonly Button[] 사용버튼들 = new Button[전장식량.종류수];
    readonly Button[] 조리버튼들 = new Button[2];
    readonly Action[] 사용콜백들 = new Action[전장식량.종류수];
    readonly Action[] 조리콜백들 = new Action[2];
    public bool 열림 { get; private set; }

    public 전장식량창(VisualElement 뿌리, Font 글꼴, Sprite[] 아이콘들, Action<int> 굽기, Action<int> 사용)
    {
        버튼 = 뿌리.Q<Button>("food-toggle");
        패널 = 뿌리.Q("food-panel");
        상태글 = 패널.Q<Label>("food-status");
        if (글꼴 != null)
        {
            버튼.style.unityFont = 글꼴;
            패널.style.unityFont = 글꼴;
        }
        버튼.clicked += 전환;
        for (int i = 0; i < 수량들.Length; i++)
        {
            int 종류 = i;
            패널.Q($"food-icon-{i}").style.backgroundImage = new StyleBackground(아이콘들[i]);
            패널.Q<Label>($"food-recovery-{i}").text = $"왕·아군 회복 +{전장식량.회복량[i]}";
            수량들[i] = 패널.Q<Label>($"food-count-{i}");
            사용버튼들[i] = 패널.Q<Button>($"food-use-{i}");
            사용콜백들[i] = () => 사용(종류);
            사용버튼들[i].clicked += 사용콜백들[i];
            if (i >= 조리버튼들.Length) continue;
            조리버튼들[i] = 패널.Q<Button>($"food-cook-{i}");
            조리콜백들[i] = () => 굽기(종류);
            조리버튼들[i].clicked += 조리콜백들[i];
        }
    }

    public void 전환() => 열림 = !열림;
    public void 열기() => 열림 = true;
    public void 닫기() => 열림 = false;

    public void 갱신(진영상태 팀, bool 표시)
    {
        버튼.style.display = 표시 ? DisplayStyle.Flex : DisplayStyle.None;
        패널.style.display = 표시 && 열림 ? DisplayStyle.Flex : DisplayStyle.None;
        if (!표시 || !열림) return;
        for (int i = 0; i < 수량들.Length; i++)
        {
            int 수량 = 팀.고기 != null && i < 팀.고기.Length ? 팀.고기[i] : 0;
            수량들[i].text = 수량.ToString();
            사용버튼들[i].SetEnabled(수량 > 0);
            if (i < 조리버튼들.Length) 조리버튼들[i].SetEnabled(수량 > 0 && 팀.조리중인고기 < 0);
        }
        상태글.text = 팀.조리중인고기 >= 0
            ? $"굽는 중   {Mathf.CeilToInt(팀.조리남은시간)}초"
            : "완공된 아군 모닥불 가까이에서 굽기";
    }

    public void Dispose()
    {
        버튼.clicked -= 전환;
        for (int i = 0; i < 사용버튼들.Length; i++) 사용버튼들[i].clicked -= 사용콜백들[i];
        for (int i = 0; i < 조리버튼들.Length; i++) 조리버튼들[i].clicked -= 조리콜백들[i];
        버튼.style.display = DisplayStyle.None;
        패널.style.display = DisplayStyle.None;
    }
}
