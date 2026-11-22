using UnityEngine;
using UnityEngine.UIElements;

public sealed partial class 신들의전쟁
{
    [SerializeField] SpriteRenderer[] 깃발슬롯;
    [SerializeField] 메뉴버튼 깃발버튼;
    Sprite[] 깃발그림;

    void 깃발슬롯준비()
    {
        깃발그림 = Resources.LoadAll<Sprite>("Buildings/flags");
        if (깃발슬롯 != null)
            for (int i = 0; i < 깃발슬롯.Length; i++)
            {
                깃발슬롯[i].enabled = false;
                슬롯[전장규칙.깃발시작칸 + i] = 깃발슬롯[i];
            }
        if (깃발버튼 != null)
        {
            깃발버튼.onClick.RemoveListener(깃발배치선택);
            깃발버튼.onClick.AddListener(깃발배치선택);
        }
    }
    void 깃발배치선택() => 행동고르기(전장규칙.깃발건설);

    void 건설영역그리기(Painter2D 펜)
    {
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 is not { 생존: true } || 대상.팀 != 내팀
                || 대상.종류 != 대상종류.회관 && (대상.종류 != 대상종류.깃발 || 대상.완공대기 > 0)) continue;
            const int 면수 = 48;
            펜.BeginPath();
            for (int i = 0; i <= 면수; i++)
            {
                float 각 = i * Mathf.PI * 2 / 면수;
                var 점 = 위치변환(대상.위치 + new Vector2(Mathf.Cos(각), Mathf.Sin(각)) * 전장규칙.건설반경);
                if (i == 0) 펜.MoveTo(점); else 펜.LineTo(점);
            }
            펜.ClosePath();
            펜.fillColor = new Color(.27f, .72f, .53f, .035f); 펜.Fill();
            펜.strokeColor = new Color(.52f, .88f, .66f, .6f); 펜.lineWidth = 1.5f; 펜.Stroke();
        }
    }
}
