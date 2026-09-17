using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OzGameLab01.Board.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Map;
using UnityEngine;

namespace OzGameLab01.Board.Views
{
    /// <summary>
    /// 타일 화면의 생성, 조회, 제거와 확대 연출을 담당합니다.
    /// </summary>
    public sealed class BoardMapView
    {
        private readonly Transform _root;
        private readonly Dictionary<MapNode, GameObject> _nodeViews = new Dictionary<MapNode, GameObject>();

        public BoardMapView(Transform root)
        {
            _root = root;
        }

        public GameObject GetNodeView(MapNode node)
        {
            if (node == null)
            {
                return null;
            }
            _nodeViews.TryGetValue(node, out GameObject view);
            return view;
        }

        public void Clear()
        {
            foreach (GameObject view in _nodeViews.Values)
            {
                if (view != null)
                {
                    Object.Destroy(view);
                }
            }
            _nodeViews.Clear();
        }

        public void Remove(MapNode node)
        {
            GameObject view = GetNodeView(node);
            if (view != null)
            {
                Object.Destroy(view);
            }
            _nodeViews.Remove(node);
        }

        public Transform CreateNode(MapNode node, MapThemeData theme, float spacing, float scale, IBoardTileInput input)
        {
            Vector3 position = new Vector3(node.Position.x * spacing, 0f, node.Position.y * spacing);
            GameObject normalPrefab = GetRandomNormalPrefab(theme);
            GameObject basePrefab = GetBasePrefabForType(node.Type, theme, normalPrefab);
            GameObject objectPrefab = GetObjectPrefabForType(node.Type, theme);

            GameObject baseView = CreateTileInstance(basePrefab, position, scale, false);
            Vector3 objectPosition = position + Vector3.up * theme.ObjectHeightOffset;
            float objectScale = scale * Mathf.Max(0.01f, theme.ObjectScaleMultiplier);
            GameObject objectView = CreateTileInstance(objectPrefab, objectPosition, objectScale, true);

            if (baseView == null && objectView == null)
            {
                return null;
            }

            GameObject primaryView = baseView != null ? baseView : objectView;
            _nodeViews[node] = primaryView;

            if (baseView != null && objectView != null)
            {
                objectView.transform.SetParent(baseView.transform, true);
            }

            InitializeTileView(baseView, node, input);
            InitializeTileView(objectView, node, input);
            return primaryView.transform;
        }

        private GameObject CreateTileInstance(GameObject prefab, Vector3 position, float scale, bool useUniformScale)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, position, prefab.transform.rotation, _root);
            bool containsSprite = instance.GetComponentInChildren<SpriteRenderer>(true) != null;
            instance.transform.localScale = useUniformScale || containsSprite
                ? instance.transform.localScale * scale
                : Vector3.Scale(instance.transform.localScale, new Vector3(scale, 1f, scale));
            return instance;
        }

        private static void InitializeTileView(GameObject view, MapNode node, IBoardTileInput input)
        {
            if (view == null)
            {
                return;
            }

            TileView tile = view.GetComponent<TileView>();
            if (tile != null)
            {
                tile.Init(node);
                tile.BindInput(input);
            }
        }

        private static GameObject GetBasePrefabForType(NodeType type, MapThemeData theme, GameObject normalPrefab)
        {
            switch (type)
            {
                case NodeType.Start:
                case NodeType.Normal:
                    return normalPrefab;
                case NodeType.Boss:
                    return ResolveSpecialBase(theme.BossBasePrefab, theme.BossObjectPrefab, theme.BossPrefab, normalPrefab);
                case NodeType.Shop:
                    return ResolveSpecialBase(theme.ShopBasePrefab, theme.ShopObjectPrefab, theme.ShopPrefab, normalPrefab);
                case NodeType.Event:
                    return ResolveSpecialBase(theme.EventBasePrefab, theme.EventObjectPrefab, theme.EventPrefab, normalPrefab);
                case NodeType.Elite:
                    return ResolveSpecialBase(theme.MidBossBasePrefab, theme.MidBossObjectPrefab, theme.ElitePrefab, normalPrefab);
                case NodeType.Battle:
                    return ResolveSpecialBase(theme.BattleBasePrefab, theme.BattleObjectPrefab, theme.BattlePrefab, normalPrefab);
                case NodeType.UnitAcquisition:
                    return ResolveSpecialBase(theme.UnitAcqBasePrefab, theme.UnitAcqObjectPrefab, theme.UnitAcquisitionPrefab, normalPrefab);
                case NodeType.Tree:
                    return GetRandomThemePrefab(theme.TreePrefabs) ?? normalPrefab;
                case NodeType.Rock:
                    return GetRandomThemePrefab(theme.RockPrefabs) ?? normalPrefab;
                case NodeType.WaterPuddle:
                    return theme.WaterPuddlePrefab != null ? theme.WaterPuddlePrefab : normalPrefab;
                case NodeType.WaterStart:
                    return theme.WaterStartPrefab != null ? theme.WaterStartPrefab : normalPrefab;
                case NodeType.WaterEnd:
                    return theme.WaterEndPrefab != null ? theme.WaterEndPrefab : normalPrefab;
                case NodeType.WaterBody:
                    return GetRandomThemePrefab(theme.WaterBodyPrefabs) ?? normalPrefab;
                default:
                    return normalPrefab;
            }
        }

        private static GameObject GetObjectPrefabForType(NodeType type, MapThemeData theme)
        {
            switch (type)
            {
                case NodeType.Boss: return theme.BossObjectPrefab;
                case NodeType.Shop: return theme.ShopObjectPrefab;
                case NodeType.Event: return theme.EventObjectPrefab;
                case NodeType.Elite: return theme.MidBossObjectPrefab;
                case NodeType.Battle: return theme.BattleObjectPrefab;
                case NodeType.UnitAcquisition: return theme.UnitAcqObjectPrefab;
                default: return null;
            }
        }

        private static GameObject ResolveSpecialBase(
            GameObject basePrefab,
            GameObject objectPrefab,
            GameObject legacyPrefab,
            GameObject normalFallback)
        {
            bool usesSplitVisual = basePrefab != null || objectPrefab != null;
            return usesSplitVisual
                ? basePrefab != null ? basePrefab : normalFallback
                : legacyPrefab != null ? legacyPrefab : normalFallback;
        }

        private static GameObject GetRandomNormalPrefab(MapThemeData theme)
        {
            if (!HasWeightedNormalPrefab(theme))
            {
                return theme.NormalPrefab;
            }

            int totalWeight = theme.NormalPrefabs
                .Where(tile => tile != null && tile.Prefab != null && tile.Weight > 0)
                .Sum(tile => tile.Weight);
            int randomValue = Random.Range(0, totalWeight);

            foreach (WeightedTile tile in theme.NormalPrefabs)
            {
                if (tile == null || tile.Prefab == null || tile.Weight <= 0)
                {
                    continue;
                }

                if (randomValue < tile.Weight)
                {
                    return tile.Prefab;
                }

                randomValue -= tile.Weight;
            }

            return theme.NormalPrefab;
        }

        public static bool HasWeightedNormalPrefab(MapThemeData theme)
        {
            return theme != null && theme.NormalPrefabs != null &&
                   theme.NormalPrefabs.Any(tile => tile != null && tile.Prefab != null && tile.Weight > 0);
        }

        private static GameObject GetRandomThemePrefab(List<GameObject> prefabs)
        {
            if (prefabs == null)
            {
                return null;
            }

            List<GameObject> validPrefabs = prefabs.Where(prefab => prefab != null).ToList();
            return validPrefabs.Count > 0
                ? validPrefabs[Random.Range(0, validPrefabs.Count)]
                : null;
        }

        public IEnumerator ScaleUpNode(Transform nodeTransform, float duration)
        {
            float time = 0f;
            Vector3 targetScale = nodeTransform.localScale;
            nodeTransform.localScale = Vector3.zero;

            while (time < duration)
            {
                if (nodeTransform == null) yield break;
                time += Time.deltaTime;
                float t = time / duration;
                float easeOutT = t * (2f - t);
                nodeTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easeOutT);
                yield return null;
            }

            if (nodeTransform != null) nodeTransform.localScale = targetScale;
        }

    }
}
