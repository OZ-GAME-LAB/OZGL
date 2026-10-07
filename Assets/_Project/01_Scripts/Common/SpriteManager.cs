using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OzGameLab01.Managers
{
    public static class SpriteManager
    {
        private static readonly Dictionary<string, Sprite> _spriteDict = new();
        private static readonly Dictionary<string, AsyncOperationHandle<Sprite>> _spriteHandles = new();

        /// <summary>
        /// 유물 스프라이트를 주소 기반으로 선로드한다.
        /// </summary>
        public static async Task LoadAllSpritesAsync()
        {
            await PreloadSpritesByLabelAsync("relic");
        }

        /// <summary>
        /// 게임 시작 시 특정 Label에 속한 모든 스프라이트 비동기 일괄 로드 및 캐시
        /// </summary>
        /// <param name="label">Addressables 에셋에 설정한 레이블</param>
        public static async Task PreloadSpritesByLabelAsync(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                Debug.LogWarning("[SpriteManager] 전달된 라벨이 비어 있습니다.");
                return;
            }

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(label, typeof(Sprite));
            try
            {
                IList<IResourceLocation> locations = await locationsHandle.Task;
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locations == null)
                {
                    Debug.LogError($"[SpriteManager] '{label}' 스프라이트 위치 조회 실패");
                    return;
                }

                int loadedCount = 0;
                foreach (IResourceLocation location in locations)
                {
                    string address = location.PrimaryKey;
                    if (string.IsNullOrEmpty(address) || _spriteDict.ContainsKey(address))
                        continue;

                    AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(location);
                    Sprite sprite = await spriteHandle.Task;
                    if (spriteHandle.Status == AsyncOperationStatus.Succeeded && sprite != null)
                    {
                        if (_spriteDict.ContainsKey(address))
                        {
                            Addressables.Release(spriteHandle);
                            continue;
                        }
                        _spriteDict[address] = sprite;
                        _spriteHandles[address] = spriteHandle;
                        loadedCount++;
                    }
                    else
                    {
                        Addressables.Release(spriteHandle);
                        Debug.LogError($"[SpriteManager] 스프라이트 선로드 실패: {address}");
                    }
                }
                Debug.Log($"[SpriteManager] '{label}' 스프라이트 {loadedCount}개 캐싱 완료");
            }
            finally
            {
                Addressables.Release(locationsHandle);
            }
        }

        /// <summary>
        /// 어드레서블 주소 기반 스프라이트 반환
        /// (캐시 미존재 시 개별 로드)
        /// </summary>
        /// <param name="spriteAddress">검색할 스프라이트 어드레서블 주소</param>
        public static async Task<Sprite> GetSpriteAsync(string spriteAddress)
        {
            // 전달 주소값 없으면 null 반환
            if (string.IsNullOrEmpty(spriteAddress))
            {
                Debug.LogWarning("[SpriteManager] 전달된 spriteAddress가 비어있습니다.");
                return null;
            }

            // 1. 캐시된 스프라이트 발견 시 즉시 반환
            if (_spriteDict.TryGetValue(spriteAddress, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            // 2. 어드레서블 비동기 로드 실행 (초기에 캐싱되지 않은 경우)
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteAddress);
            Sprite loadedSprite = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && loadedSprite != null)
            {
                if (_spriteDict.TryGetValue(spriteAddress, out cachedSprite))
                {
                    Addressables.Release(handle);
                    return cachedSprite;
                }
                _spriteDict[spriteAddress] = loadedSprite;
                _spriteHandles[spriteAddress] = handle;
                return loadedSprite;
            }

            // 3. 그 외 없으면 로드 실패
            Addressables.Release(handle);
            Debug.LogError($"[SpriteManager] 스프라이트 로드 실패: {spriteAddress}");
            return null;
        }

        /// <summary>
        /// 메모리 정리 시 스프라이트 캐시 초기화
        /// </summary>
        public static void ClearCache()
        {
            foreach (AsyncOperationHandle<Sprite> handle in _spriteHandles.Values)
                Addressables.Release(handle);
            _spriteHandles.Clear();
            _spriteDict.Clear();
        }
    }
}
