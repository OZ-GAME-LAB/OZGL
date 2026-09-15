using System.Threading.Tasks;

namespace OzGameLab01.Data
{
    /// <summary>
    /// 주소로부터 JSON 텍스트를 가져오는 기능을 정의합니다.
    /// </summary>
    public interface IJsonAssetLoader
    {
        Task<string> LoadAsync(string address);
    }
}
