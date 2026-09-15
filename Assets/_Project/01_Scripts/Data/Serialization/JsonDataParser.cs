using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OzGameLab01.Data
{
    /// <summary>
    /// Unity 리소스와 캐시에 의존하지 않고 JSON 목록을 역직렬화합니다.
    /// </summary>
    public static class JsonDataParser
    {
        public static List<T> Parse<T, TList>(string json) where TList : IDataList<T>
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON 내용이 비어 있습니다.", nameof(json));
            }
            TList container = JsonConvert.DeserializeObject<TList>(json);
            if (container == null)
            {
                return null;
            }
            return container.GetList();
        }
    }
}
