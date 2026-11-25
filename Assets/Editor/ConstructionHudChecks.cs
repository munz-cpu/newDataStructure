using System;
using UnityEditor;
using UnityEngine;
using TMPro;
using System.Reflection;

public static class ConstructionHudChecks
{
    [MenuItem("DivineWar/Validation/Construction HUD")]
    public static void Run()
    {
        int checks = 0;
        void Require(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        int[] kinds = { 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        int[] limits = { 2, 2, 12, 12, 2, 2, 2, 4, 8, 8, 2 };
        대상종류[] types = { 대상종류.제재소, 대상종류.훈련소, 대상종류.나무울타리, 대상종류.돌울타리,
            대상종류.모닥불, 대상종류.브루저훈련소, 대상종류.아처훈련소, 대상종류.아처타워,
            대상종류.지뢰, 대상종류.점프패드, 대상종류.깃발 };
        for (int i = 0; i < kinds.Length; i++)
        {
            var unit = new 전장대상 { 팀 = 0, 종류 = types[i], 체력 = 100, 완공대기 = 4 };
            var state = new 전장상태 { 대상들 = new[] { unit, null,
                new 전장대상 { 팀 = 1, 종류 = types[i], 체력 = 100 },
                new 전장대상 { 팀 = 0, 종류 = types[i], 체력 = 0 } } };
            Require(전장규칙.건설정원(kinds[i]) == limits[i], "Incorrect capacity: " + kinds[i]);
            Require(전장규칙.건설수량(state, 0, kinds[i]) == 1, "Count must include construction and exclude dead/enemy slots");
            unit.체력 = 0;
            Require(전장규칙.건설수량(state, 0, kinds[i]) == 0, "Destruction must release count");
            Require(전장규칙.건설수량(state, 1, kinds[i]) == 1, "Count must follow local team");
        }
        var walls = new 전장상태 { 대상들 = new[] {
            new 전장대상 { 팀 = 0, 종류 = 대상종류.나무울타리, 체력 = 100 },
            new 전장대상 { 팀 = 0, 종류 = 대상종류.돌울타리, 체력 = 100 } } };
        Require(전장규칙.건설수량(walls, 0, 4) == 2 && 전장규칙.건설수량(walls, 0, 5) == 2,
            "Both wall menus must show their shared capacity use");
        var snapshot = new 전장상태 { 진영들 = new[] {
            new 진영상태 { 표시공격력 = 28, 표시공격속도 = 1.5f, 표시이동속도 = 4.2f },
            new 진영상태 { 표시공격력 = 37, 표시공격속도 = 2.3f, 표시이동속도 = 5.1f } },
            대상들 = new[] { new 전장대상 { 체력 = 100 } } };
        var guest = JsonUtility.FromJson<전장상태>(JsonUtility.ToJson(snapshot));
        Require(guest.진영들[0].표시공격력 == 28 && guest.진영들[1].표시공격력 == 37,
            "Guest snapshot must retain each team's distinct attack damage");
        Require(guest.진영들[1].표시공격속도 == 2.3f && guest.진영들[1].표시이동속도 == 5.1f,
            "Guest snapshot must retain attack and movement speed");
        Require(!JsonUtility.ToJson(snapshot.대상들[0]).Contains("표시공격력"),
            "HUD stats must not inflate every unit's network payload");
        Debug.Log("[ConstructionHudChecks] Passed " + checks + " checks.");
    }

    [MenuItem("DivineWar/Validation/Combat Stats HUD")]
    public static void Display()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<신들의전쟁>();
        if (game == null) throw new Exception("Open Main scene before validating the combat stats HUD.");
        var field = typeof(신들의전쟁).GetField("왕능력표시", BindingFlags.Instance | BindingFlags.NonPublic);
        var text = (TMP_Text)field.GetValue(game);
        if (text == null || text.raycastTarget) throw new Exception("Combat stats text must be wired and pass pointer input.");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var minimap = (RectTransform)typeof(신들의전쟁).GetField("미니맵영역", flags).GetValue(game);
        var buildButton = ((메뉴버튼)typeof(신들의전쟁).GetField("건축버튼", flags).GetValue(game)).GetComponent<RectTransform>();
        var buildList = ((GameObject)typeof(신들의전쟁).GetField("건축목록", flags).GetValue(game)).GetComponent<RectTransform>();
        var minimapPanel = (RectTransform)buildButton.parent;
        if (!minimap.IsChildOf(minimapPanel) || buildList.parent != minimapPanel
            || buildButton.rect.width < 200 || buildButton.rect.height < 48
            || buildButton.anchoredPosition.y < minimapPanel.rect.yMax + 8
            || buildList.anchoredPosition.y < buildButton.anchoredPosition.y + buildButton.rect.height + 8)
            throw new Exception("Enlarged build toggle and list must fit above the minimap with separate hit regions.");
        var update = (Action<전장대상, 진영상태>)Delegate.CreateDelegate(typeof(Action<전장대상, 진영상태>), game, "왕능력표시갱신");
        var king = new 전장대상 { 최대체력 = 240 };
        var team = new 진영상태 { 표시공격력 = 28, 표시공격속도 = 1.5f, 표시이동속도 = 4.2f };
        update(king, team);
        if (!text.text.Contains("240") || !text.text.Contains("28")) throw new Exception("Initial combat stats were not displayed.");
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 32; i++) update(king, team);
        if (GC.GetAllocatedBytesForCurrentThread() != before) throw new Exception("Unchanged stats must not allocate HUD text.");
        team.표시공격력 = 37;
        update(king, team);
        if (!text.text.Contains("37")) throw new Exception("Changed guest/host stats must refresh the HUD.");
        Debug.Log("[ConstructionHudChecks.Display] Passed: enlarged build toggle/list separate above minimap; wired non-raycast text; snapshot values render; unchanged updates allocate zero bytes; changed values refresh.");
    }
}
