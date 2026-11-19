using System;
using UnityEngine;
using UnityEngine.UIElements;

public static class 전투글꼴
{
    static Font 글꼴;

    public static void 적용(VisualElement 뿌리)
    {
        if (글꼴 == null)
            글꼴 = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 22);
        if (글꼴 != null) 뿌리.style.unityFont = 글꼴;
    }
}

[RequireComponent(typeof(UIDocument))]
public sealed class 전투메뉴 : MonoBehaviour
{
    [SerializeField] StyleSheet 화면스타일;

    VisualElement 뿌리;
    Button 재개버튼;
    Label 제목표시, 본문표시;
    public event Action 재개, 나가기;

    void OnEnable()
    {
        뿌리 = GetComponent<UIDocument>().rootVisualElement;
        뿌리.styleSheets.Add(화면스타일);
        전투글꼴.적용(뿌리);
        제목표시 = 뿌리.Q<Label>("modal-title");
        본문표시 = 뿌리.Q<Label>("modal-copy");
        재개버튼 = 뿌리.Q<Button>("resume");
        재개버튼.clicked += () => 재개?.Invoke();
        뿌리.Q<Button>("leave").clicked += () => 나가기?.Invoke();
        숨기기();
    }

    public void 보이기(string 제목, string 본문, bool 재개가능)
    {
        제목표시.text = 제목;
        본문표시.text = 본문;
        재개버튼.style.display = 재개가능 ? DisplayStyle.Flex : DisplayStyle.None;
        뿌리.style.display = DisplayStyle.Flex;
    }

    public void 숨기기() => 뿌리.style.display = DisplayStyle.None;
}

