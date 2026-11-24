using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class 메뉴버튼 : Button
{
    [SerializeField] Color 선택색 = new(1f, .81f, .52f);
    bool 포인터안;
    Color? 기본색;

    public void 선택표시(bool 선택)
    {
        var 팔레트 = colors;
        기본색 ??= 팔레트.normalColor;
        팔레트.normalColor = 선택 ? 선택색 : 기본색.Value;
        colors = 팔레트;
    }

    public override void OnPointerEnter(PointerEventData 사건)
    {
        포인터안 = true;
        base.OnPointerEnter(사건);
    }

    public override void OnPointerExit(PointerEventData 사건)
    {
        포인터안 = false;
        base.OnPointerExit(사건);
    }

    protected override void OnDisable()
    {
        포인터안 = false;
        base.OnDisable();
    }

    protected override void DoStateTransition(SelectionState 상태, bool 즉시) =>
        base.DoStateTransition(포인터안 && 상태 == SelectionState.Selected ? SelectionState.Highlighted : 상태, 즉시);
}
