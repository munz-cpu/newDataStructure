using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public sealed partial class 신들의전쟁
{
    VisualElement 왕효과줄;
    왕효과칸[] 왕효과칸들;
    왕효과칸 설명중인효과;
    string 표시한효과제목, 표시한효과설명, 표시한효과수치;
    int 표시한효과초;

    sealed class 왕효과칸
    {
        public readonly VisualElement 영역;
        readonly VisualElement 아이콘;
        readonly Label 수치글;
        Sprite 그림;
        int 수치;
        string 형식;

        public string 제목 { get; private set; }
        public string 설명 { get; private set; }
        public string 수치표시 => 수치글.text;
        public int 남은초 { get; private set; }
        public bool 활성 => 제목 != null;

        public 왕효과칸(VisualElement 영역)
        {
            this.영역 = 영역;
            아이콘 = 영역.Q(className: "battle-status-icon");
            수치글 = 영역.Q<Label>(className: "battle-status-value");
            수치글.style.display = DisplayStyle.None;
        }

        public void 갱신(string 제목, string 설명 = null, float 시간 = 0, int 수치 = 0, string 형식 = null, Sprite 그림 = null)
        {
            var 표시상태 = 제목 != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (영역.style.display.value != 표시상태) 영역.style.display = 표시상태;
            this.제목 = 제목;
            this.설명 = 설명;
            남은초 = Mathf.CeilToInt(시간);
            if (제목 == null) return;

            if (this.그림 != 그림)
            {
                this.그림 = 그림;
                아이콘.style.backgroundImage = 그림 != null ? new StyleBackground(그림) : new StyleBackground(StyleKeyword.Null);
            }
            if (this.수치 != 수치 || this.형식 != 형식)
            {
                this.수치 = 수치;
                this.형식 = 형식;
                수치글.text = 형식 == null ? "" : string.Format(형식, 수치);
                수치글.style.display = 형식 == null ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }

    void 왕효과표시준비()
    {
        왕효과줄 = 뿌리.Q("battle-status-strip");
        왕효과줄.style.unityFont = 도움.font.sourceFontFile;
        왕효과칸들 = 왕효과줄.Children().Select(영역 => new 왕효과칸(영역)).ToArray();
        설명중인효과 = null;
    }

    void 왕효과표시갱신(진영상태 진영, 전장대상 왕)
    {
        if (!왕.생존 || 뿌리.panel == null)
        {
            표시(왕효과줄, false);
            return;
        }

        for (int 칸 = 0; 칸 < 왕직업목록.장착칸수; 칸++)
        {
            var 효과 = 진영.기술효과 != null && 칸 < 진영.기술효과.Length ? 진영.기술효과[칸] : default;
            if (!효과.활성 || !왕직업목록.장착가능(진영.직업))
            {
                왕효과칸들[칸].갱신(null);
                continue;
            }
            var 기술 = 왕직업목록.장착스킬(진영, 칸);
            var 정보 = 왕직업목록.정보(기술);
            string 형식 = 기술 switch
            {
                장착기술.연속공격 => "{0}/3",
                장착기술.집중사격 => "{0}%",
                장착기술.여왕의자격 or 장착기술.단독행동 => "+{0}%",
                장착기술.무쌍 => "{0}",
                _ => null
            };
            왕효과칸들[칸].갱신(정보.이름, 정보.설명, 효과.남은시간, 효과.수치, 형식, 왕스킬아이콘목록.가져오기(기술));
        }

        왕효과칸들[4].갱신(왕.장막안 ? "잠복" : null, "아군 장막 안에서는 은신하며 이동 속도가 10% 증가합니다.",
            그림: 왕.장막안 ? 왕스킬아이콘목록.가져오기(장착기술.잠복) : null);
        왕효과칸들[5].갱신(왕.가드 > 0 ? "가드" : null, "가드가 피해를 흡수합니다. 남은 가드는 시간이 지나면서 줄어듭니다.",
            수치: Mathf.CeilToInt(왕.가드), 형식: "{0}");
        왕효과칸들[6].갱신(왕.보호량 > 0 ? "보호막" : null, "보호막이 남아 있는 동안 피해를 흡수합니다.",
            왕.보호시간, Mathf.CeilToInt(왕.보호량), "{0}");
        왕효과칸들[7].갱신(왕.방패시간 > 0 ? "방패" : null, "받는 피해가 감소합니다.", 왕.방패시간);
        왕효과칸들[8].갱신(왕.무적 > 0 ? "무적" : null, "피해를 받지 않습니다.", 왕.무적);
        왕효과칸들[9].갱신(왕.불사 > 0 ? "불사" : null, "체력이 1 아래로 내려가지 않습니다.", 왕.불사);
        왕효과칸들[10].갱신(왕.가속 > 0 ? "가속" : null, "이동 속도가 증가합니다.", 왕.가속);
        왕효과칸들[11].갱신(왕.강화시간 > 0 ? "강화" : null, "직업 스킬에 따른 강화 효과가 적용 중입니다.", 왕.강화시간);
        왕효과칸들[12].갱신(진영.시련유물 > 0 ? "시련 유물" : null, "유물 하나마다 힘이 4, 최대 체력이 40 증가합니다.",
            수치: 진영.시련유물, 형식: "{0}");
        왕효과칸들[13].갱신(왕.연속시간 > 0 && 왕.연속횟수 > 0 ? "연속 시전" : null,
            진영.직업 == 왕직업.헤카림 ? "적중으로 쌓인 중첩만큼 회전 베기의 재사용 대기 시간이 줄어듭니다."
                : "남은 횟수만큼 스킬을 연속 시전할 수 있습니다.",
            왕.연속시간, 왕.연속횟수, "{0}");

        bool 활성 = false;
        foreach (var 칸 in 왕효과칸들) 활성 |= 칸.활성;
        표시(왕효과줄, 활성);
        if (!활성) return;

        var 위치 = 왕패널위치();
        왕효과줄.style.left = Mathf.Max(8, 위치.x);
        왕효과줄.style.bottom = 뿌리.contentRect.height - 위치.y + 4;
    }

    Vector2 왕패널위치()
    {
        var 패널 = (RectTransform)왕직업글.rectTransform.parent;
        var 모서리 = 패널.TransformPoint(new Vector3(패널.rect.xMin, 패널.rect.yMax));
        return 화면변환(RectTransformUtility.WorldToScreenPoint(null, 모서리));
    }

    bool 왕효과설명표시(Vector2 위치)
    {
        if (왕효과줄.style.display.value == DisplayStyle.None) return false;
        var 가리킨칸 = 뿌리.panel.Pick(위치);
        foreach (var 칸 in 왕효과칸들)
        {
            if (!칸.활성 || 칸.영역 != 가리킨칸) continue;
            설명중인스킬 = -1;
            if (설명중인효과 != 칸 || 표시한효과제목 != 칸.제목 || 표시한효과설명 != 칸.설명
                || 표시한효과수치 != 칸.수치표시 || 표시한효과초 != 칸.남은초)
            {
                설명중인효과 = 칸;
                표시한효과제목 = 칸.제목;
                표시한효과설명 = 칸.설명;
                표시한효과수치 = 칸.수치표시;
                표시한효과초 = 칸.남은초;
                스킬제목.text = string.IsNullOrEmpty(칸.수치표시) ? 칸.제목 : $"{칸.제목}  ·  {칸.수치표시}";
                스킬설명글.text = 칸.남은초 > 0 ? $"{칸.설명}\n남은 시간 {칸.남은초}초" : 칸.설명;
            }
            왕하단설명배치(칸.영역.worldBound.xMin);
            return true;
        }
        return false;
    }

    void 왕하단설명배치(float 왼쪽)
    {
        var 화면 = 뿌리.contentRect;
        float 설명너비 = Mathf.Min(326, 화면.width * .86f);
        float 기준높이 = 왕패널위치().y;
        if (왕효과줄.style.display.value != DisplayStyle.None && 왕효과줄.resolvedStyle.height > 0)
            기준높이 = Mathf.Min(기준높이, 왕효과줄.worldBound.yMin);
        스킬설명상자.style.left = Mathf.Clamp(왼쪽, 8, Mathf.Max(8, 화면.width - 설명너비 - 8));
        스킬설명상자.style.bottom = 화면.height - 기준높이 + 8;
        표시(스킬설명상자, true);
    }
}
