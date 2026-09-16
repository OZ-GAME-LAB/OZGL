using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace OzGameLab01.Data
{
    /// <summary>
    /// Addressables 텍스트를 읽고 성공 및 실패 경로에서 핸들을 해제합니다.
    /// </summary>
    public sealed class AddressableJsonAssetLoader : IJsonAssetLoader
    {
        public async Task<string> LoadAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("JSON Addressables 주소가 비어 있습니다.", nameof(address));
            }
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            try
            {
                TextAsset asset = await handle.Task;
                if (asset == null)
                {
                    throw new InvalidOperationException("JSON 에셋을 찾을 수 없습니다: " + address);
                }
                return asset.text;
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}
