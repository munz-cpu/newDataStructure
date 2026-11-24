using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class 스킬선택버튼 : Button
{
    [SerializeField] 스킬윤곽선 윤곽선;
    bool 장착중, 선택중, 포인터안;
    public event Action 호버시작;

    public void 표시(Sprite 아이콘, bool 장착, bool 선택)
    {
        image.sprite = 아이콘;
        image.enabled = 아이콘 != null;
        장착중 = 장착;
        선택중 = 선택;
        윤곽갱신();
    }

    public override void OnPointerEnter(PointerEventData 사건)
    {
        포인터안 = true;
        base.OnPointerEnter(사건);
        if (IsInteractable()) 호버시작?.Invoke();
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

    protected override void DoStateTransition(SelectionState 상태, bool 즉시)
    {
        base.DoStateTransition(상태, 즉시);
        윤곽갱신();
    }

    void 윤곽갱신()
    {
        if (윤곽선 != null) 윤곽선.상태표시(장착중, 선택중, 포인터안 && IsInteractable());
    }
}
