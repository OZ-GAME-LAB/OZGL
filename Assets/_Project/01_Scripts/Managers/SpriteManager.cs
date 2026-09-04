using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OzGameLab01.Managers
{
    public static class SpriteManager
    {
        private static readonly Dictionary<string, Sprite> _spriteDict = new();

        /// <summary>
        /// === 초기 로딩 시퀀스에서 호출 ===
        /// 모든 스프라이트 비동기 일괄 로드 및 캐싱
        /// </summary>
        /// <returns></returns>
        public static async Task LoadAllSpritesAsync()
        {
            await Task.WhenAll(
                PreloadSpritesByLabelAsync("")
                );
        }

        /// <summary>
        /// 게임 시작 시 특정 Label에 속한 모든 스프라이트 비동기 일괄 로드 및 캐시
        /// </summary>
        /// <param name="label">Addressables Groups에 설정한 레이블 (예: "Sprites", "Icons")</param>
        public static async Task PreloadSpritesByLabelAsync(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                Debug.LogWarning("[SpriteManager] 전달된 라벨이 비어 있습니다.");
                return;
            }

            // 레이블 기반 일괄 비동기 로드
            AsyncOperationHandle<IList<Sprite>> handle = Addressables.LoadAssetsAsync<Sprite>(label, null);
            IList<Sprite> loadedSprites = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && loadedSprites != null)
            {
                foreach (var sprite in loadedSprites)
                {
                    if (sprite != null && !_spriteDict.ContainsKey(sprite.name))
                    {
                        // 스프라이트 고유 이름 혹은 키를 기준으로 캐싱
                        _spriteDict[sprite.name] = sprite;
                    }
                }
                Debug.Log($"[SpriteManager] '{label}' 스프라이트 {loadedSprites.Count}개 캐싱 완료");
            }
            else
            {
                Debug.LogError($"[SpriteManager] '{label}' 스프라이트 사전 로드 실패");
            }
        }

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

            // 2. 어드레서블 비동기 로드 실행
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteAddress);
            Sprite loadedSprite = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && loadedSprite != null)
            {
                _spriteDict[spriteAddress] = loadedSprite;
                return loadedSprite;
            }

            // 3. 그 외 없으면 로드 실패
            Debug.LogError($"[SpriteManager] 스프라이트 로드 실패: {spriteAddress}");
            return null;
        }
    }
}