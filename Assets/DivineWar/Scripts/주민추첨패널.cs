using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 주민추첨패널
{
    static readonly Color32 금색 = new(240, 199, 119, 255);
    static readonly Color32 밝은색 = new(244, 235, 216, 255);
    static readonly Color32 흐린색 = new(201, 181, 150, 255);
    static readonly Color32 실패색 = new(248, 143, 126, 255);
    static readonly int[] 투자순서 = { 3, 0, 4, 1, 5, 2, 6 };

    readonly RectTransform 가림막, 상자, 진행선;
    readonly RectTransform 진행선바탕;
    readonly TextMeshProUGUI 보유표시, 비용표시, 능력치표시, 인원표시, 결과표시, 투자문구;
    readonly Slider 투자슬라이더;
    readonly Button 투자버튼, 인원감소, 인원증가, 인원최대;
    readonly TMP_FontAsset 글꼴;
    readonly Func<int, int, bool> 요청;
    int 모집인원 = 1;
    int 이전결과번호, 이전나무 = -1, 이전돌 = -1, 이전정원 = -1, 이전선택 = -1, 이전인원 = -1;
    bool 응답대기;
    string 전송오류;
    전장상태 표시상태;
    int 표시팀;

    public bool 유효 => 가림막 != null;
    public bool 열림 => 유효 && 가림막.gameObject.activeSelf;
    int 선택한투자위치 => Mathf.RoundToInt(투자슬라이더.value);

    public 주민추첨패널(RectTransform 부모, TMP_FontAsset 글꼴, Func<int, int, bool> 요청,
        Sprite 패널그림, Sprite 입력테두리, Sprite 구분선, Sprite 일반버튼, Sprite 강조버튼)
    {
        this.글꼴 = 글꼴;
        this.요청 = 요청;
        가림막 = 배경(부모, "주민 뽑기 가림막", new Color32(18, 12, 8, 188), true).rectTransform;
        가림막.anchorMin = Vector2.zero;
        가림막.anchorMax = Vector2.one;
        가림막.offsetMin = 가림막.offsetMax = Vector2.zero;

        상자 = 배경(가림막, "주민 뽑기", Color.white, 그림: 패널그림).rectTransform;
        상자.sizeDelta = new Vector2(584, 478);
        글(상자, "마을회관 주민 뽑기", 26, 21, 332, 39, 29, 밝은색);
        var 닫기버튼 = 버튼(상자, "닫기", 478, 26, 80, 30, 닫기, 일반버튼);
        글(닫기버튼.transform, "닫기", 0, 0, 80, 30, 16, 밝은색).alignment = TextAlignmentOptions.Center;
        보유표시 = 글(상자, "", 26, 68, 532, 28, 18, 흐린색);
        배치(배경(상자, "투자 구분선", Color.white, 그림: 구분선).rectTransform, 26, 96, 532, 9);

        var 투자상자 = 배경(상자, "투자 단계 선택", Color.white, 그림: 입력테두리).rectTransform;
        배치(투자상자, 26, 110, 240, 181);
        글(투자상자, "재료 단계", 14, 9, 212, 28, 18, 금색);
        비용표시 = 글(투자상자, "", 14, 53, 212, 26, 19, 밝은색);
        투자슬라이더 = 슬라이더만들기(투자상자);
        투자슬라이더.onValueChanged.AddListener(_ => 전송오류 = null);

        var 능력치상자 = 배경(상자, "주민 스탯 범위", Color.white, 그림: 입력테두리).rectTransform;
        배치(능력치상자, 282, 110, 276, 181);
        글(능력치상자, "주민별 랜덤 스탯", 14, 9, 248, 28, 18, 금색);
        능력치표시 = 글(능력치상자, "", 14, 42, 248, 129, 17, 밝은색);
        능력치표시.alignment = TextAlignmentOptions.TopLeft;

        var 인원상자 = 배경(상자, "모집 인원 선택", Color.white, 그림: 입력테두리).rectTransform;
        배치(인원상자, 26, 302, 532, 50);
        글(인원상자, "모집 인원", 14, 0, 212, 50, 18, 금색);
        인원감소 = 버튼(인원상자, "인원 감소", 278, 7, 46, 36, () => 인원변경(-1), 일반버튼);
        글(인원감소.transform, "-", 0, 0, 46, 36, 22, 밝은색).alignment = TextAlignmentOptions.Center;
        인원표시 = 글(인원상자, "", 328, 0, 64, 50, 20, 밝은색);
        인원표시.alignment = TextAlignmentOptions.Center;
        인원증가 = 버튼(인원상자, "인원 증가", 396, 7, 46, 36, () => 인원변경(1), 일반버튼);
        글(인원증가.transform, "+", 0, 0, 46, 36, 22, 밝은색).alignment = TextAlignmentOptions.Center;
        var 최대버튼프리팹 = Resources.Load<GameObject>("ResidentMaxButton");
        인원최대 = UnityEngine.Object.Instantiate(최대버튼프리팹, 인원상자, false).GetComponent<Button>();
        인원최대.onClick.AddListener(최대인원선택);

        결과표시 = 글(상자, "", 26, 357, 532, 45, 20, 밝은색);
        결과표시.alignment = TextAlignmentOptions.Center;
        진행선바탕 = 배경(상자, "추첨 진행", new Color32(39, 27, 20, 255)).rectTransform;
        배치(진행선바탕, 26, 404, 532, 4);
        진행선 = 배경(진행선바탕, "남은 진행", 금색).rectTransform;
        배치(진행선, 0, 0, 0, 4);
        투자버튼 = 버튼(상자, "뽑기", 26, 420, 532, 38, 투자, 강조버튼);
        투자문구 = 글(투자버튼.transform, "뽑기", 0, 0, 532, 38, 20, 밝은색);
        투자문구.alignment = TextAlignmentOptions.Center;
        닫기();
    }

    Slider 슬라이더만들기(RectTransform 부모)
    {
        const float 폭 = 212;
        const float 손잡이폭 = 12;
        var 조작영역 = 배경(부모, "투자 슬라이더", Color.clear, true).rectTransform;
        배치(조작영역, 14, 102, 폭, 44);
        배치(배경(조작영역, "슬라이더 트랙", new Color32(117, 101, 73, 255)).rectTransform,
            손잡이폭 / 2, 19, 폭 - 손잡이폭, 6);

        var 손잡이영역 = new GameObject("슬라이더 이동 범위", typeof(RectTransform)).GetComponent<RectTransform>();
        손잡이영역.SetParent(조작영역, false);
        배치(손잡이영역, 손잡이폭 / 2, 0, 폭 - 손잡이폭, 44);
        var 손잡이 = 배경(손잡이영역, "슬라이더 손잡이", Color.white);
        손잡이.rectTransform.anchorMin = 손잡이.rectTransform.anchorMax = new Vector2(0, 0.5f);
        손잡이.rectTransform.sizeDelta = new Vector2(손잡이폭, 16);

        var 슬라이더 = 조작영역.gameObject.AddComponent<Slider>();
        슬라이더.handleRect = 손잡이.rectTransform;
        슬라이더.targetGraphic = 손잡이;
        슬라이더.wholeNumbers = false;
        슬라이더.maxValue = 투자순서.Length - 1;
        var 색 = 슬라이더.colors;
        색.normalColor = 금색;
        색.highlightedColor = 색.selectedColor = 밝은색;
        색.pressedColor = 금색;
        색.disabledColor = 흐린색;
        슬라이더.colors = 색;

        for (int i = 0; i < 투자순서.Length; i++)
        {
            float 중심 = 14 + 손잡이폭 / 2 + (폭 - 손잡이폭) * (i / 슬라이더.maxValue);
            글(부모, $"{i + 1}", 중심 - 12, 147, 24, 24, 14, 흐린색).alignment = TextAlignmentOptions.Center;
        }
        return 슬라이더;
    }

    public void 열기(전장상태 상태, int 팀)
    {
        가림막.SetAsLastSibling();
        가림막.gameObject.SetActive(true);
        var 진영 = 상태.진영들[팀];
        int 진행중인선택 = Array.IndexOf(투자순서, 진영.주민투자 - 1);
        if (진행중인선택 >= 0)
        {
            투자슬라이더.SetValueWithoutNotify(진행중인선택);
            모집인원 = Mathf.Max(1, 진영.주민모집인원);
        }
        전송오류 = null;
        갱신(상태, 팀);
    }

    public void 닫기()
    {
        if (열림) 가림막.gameObject.SetActive(false);
        표시상태 = null;
    }

    void 인원변경(int 변화)
    {
        모집인원 += 변화;
        전송오류 = null;
    }

    void 최대인원선택()
    {
        if (!열림 || 응답대기 || 표시상태 == null) return;
        var 단계 = 전장규칙.주민투자목록[투자순서[선택한투자위치]];
        int 최대 = 전장규칙.주민추첨최대인원(표시상태, 표시팀, 단계);
        if (최대 <= 0) return;
        모집인원 = 최대;
        전송오류 = null;
        갱신(표시상태, 표시팀);
    }

    void 투자()
    {
        if (응답대기 || !투자버튼.interactable) return;
        응답대기 = 요청(투자순서[선택한투자위치], 모집인원);
        if (응답대기)
            투자버튼.interactable = 투자슬라이더.interactable = 인원감소.interactable = 인원증가.interactable = 인원최대.interactable = false;
        else 전송오류 = "요청을 보내지 못했습니다. 다시 시도하세요.";
    }

    public void 갱신(전장상태 상태, int 팀)
    {
        var 진영 = 상태.진영들[팀];
        if (이전결과번호 != 진영.주민추첨결과번호)
        {
            이전결과번호 = 진영.주민추첨결과번호;
            응답대기 = false;
        }
        if (!열림) return;
        표시상태 = 상태;
        표시팀 = 팀;

        var 크기 = 가림막.rect.size;
        상자.localScale = Vector3.one * Mathf.Clamp01(Mathf.Min(크기.x / 608, 크기.y / 502));
        int 빈정원 = 전장규칙.주민빈정원(상태, 팀);
        bool 추첨중 = 진영.주민투자 > 0;
        bool 선택가능 = !응답대기 && !추첨중;
        if (선택가능) 모집인원 = Mathf.Clamp(모집인원, 1, Mathf.Max(1, 빈정원));
        int 나무 = 진영.재화[0], 돌 = 진영.재화[1];
        if (이전나무 != 나무 || 이전돌 != 돌 || 이전정원 != 빈정원)
        {
            보유표시.text = $"보유  나무 {나무}  ·  돌 {돌}                         주민 빈자리 {빈정원}/{전장규칙.주민제한}";
            이전나무 = 나무;
            이전돌 = 돌;
            이전정원 = 빈정원;
        }

        int 선택 = 선택한투자위치;
        var 단계 = 전장규칙.주민투자목록[투자순서[선택]];
        if (이전선택 != 선택)
        {
            var 기본 = 전장규칙.능력치(대상종류.주민);
            능력치표시.text = $"힘  {범위(기본.힘)}\n민첩  {범위(기본.민첩)}\n지능  {전장규칙.주민추첨지능(단계.최소보정)} ~ {전장규칙.주민추첨지능(단계.최대보정)}\n체력  {범위(기본.체력, 전장규칙.주민체력보정단위)}";

            string 범위(int 기본값, int 단위 = 1)
                => $"{전장규칙.주민능력치보정(기본값, 단계.최소보정, 단위)} ~ {전장규칙.주민능력치보정(기본값, 단계.최대보정, 단위)}";
        }
        if (이전선택 != 선택 || 이전인원 != 모집인원)
        {
            var 비용 = 단계.총비용(모집인원);
            비용표시.text = 비용.돌 > 0 ? $"나무 {비용.나무}  ·  돌 {비용.돌}" : $"나무 {비용.나무}";
            인원표시.text = $"{모집인원}명";
            이전인원 = 모집인원;
            이전선택 = 선택;
        }

        string 오류 = 전장규칙.주민추첨오류(상태, 팀, 단계, 모집인원);
        투자슬라이더.interactable = 선택가능;
        인원감소.interactable = 선택가능 && 모집인원 > 1;
        인원증가.interactable = 선택가능 && 모집인원 < 빈정원;
        인원최대.interactable = 선택가능 && 전장규칙.주민추첨최대인원(상태, 팀, 단계) > 0;
        투자버튼.interactable = 선택가능 && 오류 == null;
        투자문구.text = 선택가능 ? 오류 ?? $"{모집인원}명 뽑기" : "처리 중";
        진행선바탕.gameObject.SetActive(추첨중);
        if (추첨중)
        {
            float 진행도 = 1 - Mathf.Clamp01(진영.모집대기 / 전장규칙.주민추첨시간);
            진행선.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 532 * 진행도);
        }

        if (추첨중 || 응답대기)
        {
            결과표시.text = 추첨중 ? "추첨 중…" : "요청 중…";
            결과표시.color = 금색;
        }
        else
        {
            결과표시.text = 전송오류 ?? 진영.주민추첨결과 ?? "";
            결과표시.color = 전송오류 != null || 진영.주민추첨인원 < 0 ? 실패색 : 금색;
        }
    }

    static void 배치(RectTransform 영역, float x, float y, float 폭, float 높이)
    {
        영역.anchorMin = 영역.anchorMax = new Vector2(0, 1);
        영역.pivot = new Vector2(0, 1);
        영역.anchoredPosition = new Vector2(x, -y);
        영역.sizeDelta = new Vector2(폭, 높이);
    }

    static Image 배경(Transform 부모, string 이름, Color 색, bool 클릭대상 = false, Sprite 그림 = null)
    {
        var 이미지 = new GameObject(이름, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        이미지.transform.SetParent(부모, false);
        이미지.color = 색;
        이미지.sprite = 그림;
        if (그림 != null && 그림.border != Vector4.zero) 이미지.type = Image.Type.Sliced;
        이미지.raycastTarget = 클릭대상;
        return 이미지;
    }

    Button 버튼(Transform 부모, string 이름, float x, float y, float 폭, float 높이, Action 클릭, Sprite 그림)
    {
        var 배경그림 = 배경(부모, 이름, Color.white, true, 그림);
        배경그림.pixelsPerUnitMultiplier = 1.35f;
        배치(배경그림.rectTransform, x, y, 폭, 높이);
        var 버튼 = 배경그림.gameObject.AddComponent<Button>();
        버튼.targetGraphic = 배경그림;
        버튼.navigation = new Navigation { mode = Navigation.Mode.None };
        버튼.onClick.AddListener(() => 클릭());
        return 버튼;
    }

    TextMeshProUGUI 글(Transform 부모, string 내용, float x, float y, float 폭, float 높이, int 크기, Color 색)
    {
        var 표시 = new GameObject("문구", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        표시.transform.SetParent(부모, false);
        배치(표시.rectTransform, x, y, 폭, 높이);
        표시.font = 글꼴;
        표시.fontSize = 크기;
        표시.color = 색;
        표시.alignment = TextAlignmentOptions.MidlineLeft;
        표시.raycastTarget = false;
        표시.text = 내용;
        return 표시;
    }
}
