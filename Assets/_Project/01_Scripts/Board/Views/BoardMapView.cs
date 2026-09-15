using System.Collections;
using System.Collections.Generic;
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
            GameObject prefab = GetPrefabForType(node.Type, theme) ?? theme.NormalPrefab;
            if (prefab == null)
            {
                return null;
            }
            Vector3 position = new Vector3(node.Position.x * spacing, 0f, node.Position.y * spacing);
            GameObject view = Object.Instantiate(prefab, position, Quaternion.identity, _root);
            _nodeViews[node] = view;
            view.transform.localScale = Vector3.Scale(view.transform.localScale, new Vector3(scale, 1f, scale));
            TileView tile = view.GetComponent<TileView>();
            if (tile != null)
            {
                tile.Init(node);
                tile.BindInput(input);
            }
            return view.transform;
        }

        private GameObject GetPrefabForType(NodeType type, MapThemeData theme)
        {
            switch (type)
            {
                case NodeType.Start:
                case NodeType.Normal: return theme.NormalPrefab;
                case NodeType.Boss: return theme.BossPrefab;
                case NodeType.Shop: return theme.ShopPrefab;
                case NodeType.Event: return theme.EventPrefab;
                case NodeType.Elite: return theme.ElitePrefab;
                case NodeType.Battle: return theme.BattlePrefab;
                case NodeType.UnitAcquisition: return theme.UnitAcquisitionPrefab != null ? theme.UnitAcquisitionPrefab : theme.NormalPrefab; // [추가됨] 유닛 획득 타일

                case NodeType.Tree: return GetRandomPrefab(theme.TreePrefabs, theme.NormalPrefab);
                case NodeType.Rock: return GetRandomPrefab(theme.RockPrefabs, theme.NormalPrefab);

                case NodeType.WaterPuddle: return theme.WaterPuddlePrefab != null ? theme.WaterPuddlePrefab : theme.NormalPrefab;
                case NodeType.WaterStart: return theme.WaterStartPrefab != null ? theme.WaterStartPrefab : theme.NormalPrefab;
                case NodeType.WaterEnd: return theme.WaterEndPrefab != null ? theme.WaterEndPrefab : theme.NormalPrefab;
                case NodeType.WaterBody: return GetRandomPrefab(theme.WaterBodyPrefabs, theme.NormalPrefab);

                default: return theme.NormalPrefab;
            }
        }

        private GameObject GetRandomPrefab(List<GameObject> prefabs, GameObject fallback)
        {
            if (prefabs == null || prefabs.Count == 0) return fallback;
            return prefabs[Random.Range(0, prefabs.Count)];
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
