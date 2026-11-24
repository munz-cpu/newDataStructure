using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class BattlefieldLayoutChecks
{
    [MenuItem("DivineWar/Validation/Battlefield Layout")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Main.unity");
        try
        {
            var game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<신들의전쟁>(true)).Single();
            T Field<T>(string name) => (T)typeof(신들의전쟁).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game);
            var ground = Field<Tilemap>("바닥타일맵");
            var obstacles = Field<Tilemap>("장애물타일맵");
            var overlay = Field<Tilemap>("지형타일맵");
            var resources = Field<자원지점[]>("자원들");
            var starts = Array.ConvertAll(Field<Transform[]>("시작지점들"), point => (Vector2)point.position);
            var markers = Array.ConvertAll(Field<시련표식[]>("시련들"), marker => (Vector2)marker.transform.position);
            var tree = Field<SpriteRenderer>("숲나무프리팹");
            var assembly = typeof(격자길찾기).Assembly;
            var terrainType = assembly.GetType("전장지형");
            using var terrain = (IDisposable)Activator.CreateInstance(terrainType, ground, obstacles, overlay, Field<TileBase[]>("해변타일"),
                Field<TileBase[]>("흙타일"), resources, Field<Array>("전술지형"), true, tree);
            var constructor = typeof(격자길찾기).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(Tilemap), typeof(Tilemap), terrainType }, null);
            var canBuild = typeof(격자길찾기).GetMethod("건설지형가능", BindingFlags.Instance | BindingFlags.NonPublic);
            var concealed = terrainType.GetMethod("은신지형인가");
            var leapCrossesForest = terrainType.GetMethod("도약이숲을가로지름");
            var arguments = new object[] { ground, obstacles, terrain };
            var map = (격자길찾기)constructor.Invoke(arguments);
            Require(map.전장범위.size == new Vector2(136, 88), "Expanded map size");
            var outerForest = new Vector3Int(-42, 14, 0);
            var forestPosition = (Vector2)ground.GetCellCenterWorld(outerForest);
            var path = new List<Vector2>();
            Vector2 Center(Vector3Int cell) => ground.GetCellCenterWorld(cell);
            Require(tree != null && !map.정적통행가능(forestPosition)
                && !(bool)canBuild.Invoke(map, new object[] { forestPosition }), "Outer forest blocks travel and construction");
            var forest = new HashSet<Vector3Int>();
            bool walkableBrush = false;
            foreach (var cell in overlay.cellBounds.allPositionsWithin)
            {
                var position = Center(cell);
                if (!(bool)concealed.Invoke(terrain, new object[] { position })) continue;
                if (map.정적통행가능(position)) walkableBrush = true;
                else forest.Add(cell);
            }
            Require(walkableBrush, "Brush remains walkable and conceals units");
            var forestTrees = overlay.GetComponentsInChildren<SpriteRenderer>()
                .ToDictionary(renderer => ground.WorldToCell(renderer.transform.position));
            Require(forest.Contains(outerForest) && forest.Count >= 40 && forest.SetEquals(forestTrees.Keys)
                && forestTrees.Values.All(renderer => renderer.sprite == tree.sprite), "A tree covers every blocked forest cell");
            var frontTree = forestTrees[outerForest + Vector3Int.down];
            var backTree = forestTrees[outerForest];
            Require(frontTree.transform.localScale.x > backTree.transform.localScale.x
                && frontTree.sortingOrder > backTree.sortingOrder, "The lower forest row is larger and renders in front");
            float betweenTrees = (frontTree.transform.position.y + backTree.transform.position.y) * .5f;
            int unitOrder = (int)(-betweenTrees * 80) + 1600;
            Require(frontTree.sortingLayerID == Field<SpriteRenderer[]>("슬롯")[0].sortingLayerID
                && backTree.sortingOrder < unitOrder && unitOrder < frontTree.sortingOrder,
                "A unit between forest rows renders in front of the upper tree and behind the lower tree");

            bool checkedCrossing = false;
            foreach (var cell in forest)
            {
                foreach (var direction in new[] { Vector3Int.right, Vector3Int.up })
                {
                    var start = Center(cell - direction);
                    if (!map.정적통행가능(start)) continue;
                    for (int distance = 1; distance < 4; distance++)
                    {
                        var end = Center(cell + direction * distance);
                        if (!map.정적통행가능(end)) continue;
                        map.구하기(start, end, path);
                        if (path.Count == 0 || path[path.Count - 1] != end) continue;
                        Require(path.All(map.정적통행가능) && path.Any(point =>
                            direction == Vector3Int.up ? point.x != start.x : point.y != start.y), "Path detours around forest");
                        Require(map.방향이동(start, Center(cell)) == start, "Direct movement stops at the forest edge");
                        Require((bool)leapCrossesForest.Invoke(terrain, new object[] { start, end })
                            && (bool)leapCrossesForest.Invoke(terrain, new object[] { end, start }), "Leaps cannot cross the forest in either direction");
                        Require(!(bool)leapCrossesForest.Invoke(terrain, new object[] { start, path[0] }), "A forest-free trajectory is allowed");
                        checkedCrossing = true;
                        break;
                    }
                    if (checkedCrossing) break;
                }
                if (checkedCrossing) break;
            }
            Require(checkedCrossing, "An accessible forest crossing shorter than a full leap exists on Main");

            var halls = new HashSet<Vector2>();
            var opponentsByHall = new Dictionary<Vector2, HashSet<Vector2>>();
            var quadrantPairs = new HashSet<(int, int)>();
            int Quadrant(Vector2 point) => (point.x > 0 ? 1 : 0) + (point.y > 0 ? 2 : 0);
            float nearestTrial = float.PositiveInfinity;
            float nearestOpponent = float.PositiveInfinity;
            int sameQuadrant = 0;
            var random = new System.Random(42815);
            const int samples = 512;
            for (int i = 0; i < samples; i++)
            {
                var navigation = (격자길찾기)constructor.Invoke(arguments);
                var rules = new 전장규칙(resources, navigation, starts, 왕직업.지휘관, 왕직업.아처, random.Next());
                rules.시련배치(markers);
                var state = rules.상태;
                Require(state.시련들.Length == markers.Length, $"Six trials in layout {i}");
                var firstHall = state.대상들[전장규칙.회관번호(0)].위치;
                var secondHall = state.대상들[전장규칙.회관번호(1)].위치;
                int firstQuadrant = Quadrant(firstHall), secondQuadrant = Quadrant(secondHall);
                float hallDistance = Vector2.Distance(firstHall, secondHall);
                Require(hallDistance >= 38, $"Halls stay separated in layout {i}");
                nearestOpponent = Mathf.Min(nearestOpponent, hallDistance);
                if (firstQuadrant == secondQuadrant) sameQuadrant++;
                quadrantPairs.Add((Mathf.Min(firstQuadrant, secondQuadrant), Mathf.Max(firstQuadrant, secondQuadrant)));
                for (int team = 0; team < 2; team++)
                {
                    var hall = team == 0 ? firstHall : secondHall;
                    var opponent = team == 0 ? secondHall : firstHall;
                    var king = state.대상들[전장규칙.왕번호(team)].위치;
                    var worker = state.대상들[전장규칙.왕번호(team) + 6].위치;
                    halls.Add(hall);
                    if (!opponentsByHall.TryGetValue(hall, out var opponents))
                        opponentsByHall.Add(hall, opponents = new HashSet<Vector2>());
                    opponents.Add(opponent);
                    Require((bool)canBuild.Invoke(navigation, new object[] { hall }) &&
                        navigation.정적통행가능(king) && navigation.정적통행가능(worker), $"Valid team {team} start in layout {i}");
                    foreach (var trial in state.시련들)
                    {
                        float distance = Vector2.Distance(hall, trial.위치);
                        nearestTrial = Mathf.Min(nearestTrial, distance);
                        Require(distance >= 전장규칙.회관과시련간격, $"Trial clear of team {team} hall in layout {i}");
                        navigation.구하기(king, trial.위치, path);
                        Require(path.Count > 0 && path[path.Count - 1] == trial.위치, $"Trial reachable by team {team} in layout {i}");
                    }
                }
                for (int a = 0; a < state.시련들.Length; a++)
                for (int b = 0; b < a; b++)
                    Require(Vector2.Distance(state.시련들[a].위치, state.시련들[b].위치) >= 8, $"Trial separation in layout {i}");
            }
            Require(halls.Count >= 150, "Hall placements vary over the expanded search area");
            Require(sameQuadrant >= samples / 32, "Halls can share a quadrant when far enough apart");
            Require(halls.Any(hall => Mathf.Abs(hall.x) < 14), "Hall candidates include valid central terrain");
            int mostOpponents = opponentsByHall.Values.Max(opponents => opponents.Count);
            Require(mostOpponents >= 3, "The same hall position can face multiple opponent positions");
            Debug.Log($"Battlefield Layout passed. {samples} layouts, {halls.Count} hall positions, up to {mostOpponents} " +
                $"opponent locations for the same hall, {quadrantPairs.Count} quadrant pairs, " +
                $"{sameQuadrant} same-quadrant layouts, nearest opponent {nearestOpponent:F2} cells, " +
                $"nearest trial {nearestTrial:F2} cells.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
