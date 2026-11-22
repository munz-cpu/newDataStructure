using UnityEngine;

public sealed partial class 신들의전쟁
{
    Sprite[] 시련석프레임, 시련보스프레임, 해골프레임;
    GUIStyle 시련글꼴;

    bool 시련클릭(Vector2 위치)
    {
        if (상태?.시련들 == null || 시련들 == null) return false;
        for (int i = 0; i < 상태.시련들.Length && i < 시련들.Length; i++)
        {
            var 시련 = 상태.시련들[i];
            int 보스칸 = 전장규칙.시련보스번호(i);
            var 보스 = 상태.대상들[보스칸];
            if (시련.단계 == 시련단계.전투 && 보스 is { 생존: true } && 시련.팀 == 내팀
                && 대상표시(보스) && Vector2.Distance(위치, 보스.위치 + Vector2.up * .7f) < 1.1f)
            {
                int[] 부대 = null;
                if (선택부대.Count > 0) { 부대 = new int[선택부대.Count]; 선택부대.CopyTo(부대); }
                명령전달(명령종류.공격, 보스.위치, 보스칸, 부대);
                return true;
            }
            if (Vector2.Distance(위치, 시련.위치 + Vector2.up * .35f) > .9f || !안개.탐사됨(시련.위치)) continue;
            if (시련.단계 is 시련단계.봉인 or 시련단계.보상)
                명령전달(명령종류.시련활성화, 시련.위치, i);
            return true;
        }
        return false;
    }

    void 시련그림갱신()
    {
        if (시련들 == null) return;
        시련석프레임 ??= Resources.LoadAll<Sprite>("Trials/trial-stone");
        for (int i = 0; i < 시련들.Length; i++)
        {
            var 표식 = 시련들[i];
            if (표식 == null) continue;
            var 시련 = i < 상태.시련들.Length ? 상태.시련들[i] : null;
            bool 보임 = 시련 != null && 안개.탐사됨(시련.위치);
            표식.봉인그림.enabled = 보임;
            if (보임)
            {
                if (시련석프레임.Length >= 4) 표식.봉인그림.sprite = 시련석프레임[(int)시련.단계];
                표식.봉인그림.color = 시련.단계 == 시련단계.완료 ? new Color(.6f, .6f, .6f) : Color.white;
                표식.봉인그림.sortingOrder = (int)(-시련.위치.y * 80) + 1599;
            }
            표식.강타그림.enabled = 보임 && 시련.강타예고 > 0 && 시련.단계 == 시련단계.전투;
            if (표식.강타그림.enabled && 상태.대상들[전장규칙.시련보스번호(i)] is { 생존: true } 보스)
            {
                표식.강타그림.transform.position = 보스.위치;
                표식.강타그림.color = new Color(1, .28f, .16f, .45f + .35f * (1 - 시련.강타예고 / 1.2f));
                표식.강타그림.sortingOrder = (int)(-보스.위치.y * 80) + 1598;
            }
        }
    }

    void 시련유닛그림(전장대상 대상, SpriteRenderer 그림)
    {
        bool 보스 = 대상.종류 == 대상종류.시련보스;
        if (보스) 시련보스프레임 ??= Resources.LoadAll<Sprite>("Trials/trial-warden");
        else 해골프레임 ??= Resources.LoadAll<Sprite>("Trials/skeleton-soldier");
        var 프레임 = 보스 ? 시련보스프레임 : 해골프레임;
        if (프레임.Length < 24) return;
        int 동작 = 상태.시간 - 대상.공격시각 < .4f && 대상.공격순번 > 0 ? 2 : 대상.이동중 ? (int)(상태.시간 * 6) % 2 : 0;
        그림.sprite = 프레임[스프라이트방향(대상.시선) * 3 + 동작];
        그림.flipX = false;
    }

    void OnGUI()
    {
        if (!전투중 || 상태 == null || 시련들 == null || 패널열림 || 전장카메라 == null) return;
        시련글꼴 ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 13, font = 도움.font.sourceFontFile, richText = false };
        for (int i = 0; i < 상태.시련들.Length; i++)
        {
            var 시련 = 상태.시련들[i];
            if (훈련비용상자 != null && 훈련비용상자.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.Flex && 시련.단계 != 시련단계.전투) continue;
            if (!안개.탐사됨(시련.위치)) continue;
            var 보스 = 상태.대상들[전장규칙.시련보스번호(i)];
            bool 전투 = 시련.단계 == 시련단계.전투 && 보스 is { 생존: true };
            if (!전투 && (내왕() is not { 생존: true } 왕 || Vector2.Distance(왕.위치, 시련.위치) > 전장규칙.시련사용거리)) continue;
            var p = 전장카메라.WorldToScreenPoint((전투 ? 보스.위치 : 시련.위치) + Vector2.up * (전투 ? 2.6f : 1.5f));
            if (p.z <= 0 || p.x < 0 || p.x > Screen.width || p.y < 100 || p.y > Screen.height) continue;
            string 문구 = 시련.단계 switch {
                시련단계.봉인 => "골렘 시련 · 무작위 보상 · 클릭",
                시련단계.전투 => 전투 ? $"망각의 파수꾼  {Mathf.CeilToInt(보스.체력)} / {보스.최대체력}" : "파수꾼 소환 중…",
                시련단계.보상 => 시련.팀 == 내팀 ? 전장규칙.시련보상제목(시련.보상) + " · 클릭하여 받기" : "상대 진영의 시련 보상",
                _ => "완료한 시련"
            };
            GUI.Box(new Rect(p.x - 130, Screen.height - p.y, 260, 26), 문구, 시련글꼴);
            if (전투)
            {
                var old = GUI.color;
                GUI.color = new Color(.16f, .07f, .05f); GUI.DrawTexture(new Rect(p.x - 125, Screen.height - p.y + 27, 250, 5), Texture2D.whiteTexture);
                GUI.color = new Color(.85f, .3f, .17f); GUI.DrawTexture(new Rect(p.x - 125, Screen.height - p.y + 27, 250 * Mathf.Clamp01(보스.체력 / 보스.최대체력), 5), Texture2D.whiteTexture);
                GUI.color = old;
            }
        }
    }
}
