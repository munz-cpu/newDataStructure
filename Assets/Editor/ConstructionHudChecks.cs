using System;
using UnityEditor;
using UnityEngine;

public static class ConstructionHudChecks
{
    [MenuItem("DivineWar/Validation/Construction HUD")]
    public static void Run()
    {
        int checks = 0;
        void Require(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        int[] kinds = { 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        int[] limits = { 2, 2, 12, 12, 1, 2, 2, 4, 8, 8, 2 };
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
        Debug.Log("[ConstructionHudChecks] Passed " + checks + " checks.");
    }

}
