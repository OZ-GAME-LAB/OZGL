using System.Collections.Generic;

namespace OzGameLab01.Data
{
    /// <summary>
    /// 기존 JSON 래퍼의 목록 반환 계약을 유지합니다.
    /// </summary>
    public interface IDataList<T>
    {
        List<T> GetList();
    }
}
