//using UnityEngine;
//using System.Collections.Generic;
//using UnityEngine.AddressableAssets;
//using System.Threading.Tasks;
//using System.Reflection;
//using Newtonsoft.Json;

//public interface IDataList<T>
//{
//    List<T> GetList();
//}

//public class GameDB<T, TList> where TList : IDataList<T>
//{
//    // Dictionary<int, T>    ->   ID 값인 int, 데이터 값 그 자체인 T
//    // ID 기반 데이터 딕셔너리
//    private Dictionary<int, T> _dataDict = new Dictionary<int, T>();

//    /// <summary>
//    /// 게임 부팅 시 주소 기반 데이터베이스 불러오기
//    /// </summary>
//    /// <param name="address"> Json 파일 주소 </param>
//    /// <returns></returns>
//    public async Task LoadAsync(string address)
//    {
//        // 1. 어드레서블 주소 기반 Json파일 찾기
//        var handle = Addressables.LoadAssetAsync<TextAsset>(address);
//        TextAsset jsonFile = await handle.Task;

//        if (jsonFile == null) return;
//        string jsonText = jsonFile.text;

//        // 2. 비동기로 데이터베이스 구성
//        await Task.Run(() =>
//        {
//            // Json 역직렬화
//            TList list = JsonConvert.DeserializeObject<TList>(jsonText);

//            if (list == null) return;

//            // 데이터 유효성 검사
//            FieldInfo idFieldInfo = typeof(T).GetField("id");
//            if (idFieldInfo == null)
//            {
//                return;
//            }

//            // 데이터베이스 구성
//            _dataDict.Clear();
//            foreach (var item in list.GetList())
//            {
//                var idValue = idFieldInfo.GetValue(item);
//                if (idValue is int inObj)
//                {
//                    _dataDict[inObj] = item;
//                }
//            }
//        });

//        // 어드레서블 핸들 실행
//        Addressables.Release(handle);
//    }

//    /// <summary>
//    /// ID 기반 데이터 반환
//    /// </summary>
//    /// <param name="id"> 가져올 데이터의 ID 값 </param>
//    /// <returns> 찾고자 하는 데이터 값 </returns>
//    public T Get(int id)
//    {
//        if (_dataDict.TryGetValue(id, out T value)) return value;
//        return default;
//    }

//    /// <summary>
//    /// 모든 데이터 불러오기
//    /// </summary>
//    /// <returns> 데이터베이스 내 모든 데이터 리스트 </returns>
//    public List<T> GetAll() => new List<T>(_dataDict.Values);
//}
