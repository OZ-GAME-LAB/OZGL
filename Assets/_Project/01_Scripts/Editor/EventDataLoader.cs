#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using OzGameLab01.Events;

namespace OzGameLab01.Editor
{
    public static class EventDataLoader
    {
        [MenuItem("Tools/DB/DataLoadEvent")]
        public static void DataLoad()
        {
            string dbPath = "Assets/_Project/05_Data/ChoiceEvent/EventDB.asset";

            EventDB eventDB = AssetDatabase.LoadAssetAtPath<EventDB>(dbPath);
            if (eventDB == null)
            {
                Debug.LogError($"[EventDataLoader] EventDB를 찾을 수 없습니다: {dbPath}");
                return;
            }

            eventDB.DeleteDB();

            string[] guids = AssetDatabase.FindAssets("t:EventSO", new[] { "Assets/_Project/05_Data/ChoiceEvent" });

            Dictionary<string, EventSO> eventList = new Dictionary<string, EventSO>();

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                EventSO tempSO = AssetDatabase.LoadAssetAtPath<EventSO>(assetPath);
                if (tempSO == null || string.IsNullOrWhiteSpace(tempSO.id) || eventList.ContainsKey(tempSO.id))
                {
                    continue;
                }

                eventList.Add(tempSO.id, tempSO);

                if (tempSO.eventCategory == EventCategory.Choice)
                {
                    eventDB.EventList_Event.Add(tempSO);
                }

                switch (tempSO.choiceCategory)
                {
                    case EventChoiceCategory.Relic:
                        eventDB.EventList_Relic.Add(tempSO);
                        break;
                    case EventChoiceCategory.Battle:
                        eventDB.EventList_Battle.Add(tempSO);
                        break;
                    case EventChoiceCategory.Quiz:
                        eventDB.EventList_Quiz.Add(tempSO);
                        break;
                    case EventChoiceCategory.Exit:
                        eventDB.EventList_Finish.Add(tempSO);
                        break;
                    default:
                        break;
                }

            }

            if (eventDB.SetEvent(eventList))
            {
                EditorUtility.SetDirty(eventDB);
                AssetDatabase.SaveAssets();
                Debug.Log($"[EventDataLoader] 이벤트 DB 삽입 완료: {eventList.Count}개");
            }
        }

        [MenuItem("Tools/DB/DataDeleteEvent")]
        public static void DataDelete()
        {
            string dbPath = "Assets/_Project/05_Data/ChoiceEvent/EventDB.asset";

            EventDB eventDB = AssetDatabase.LoadAssetAtPath<EventDB>(dbPath);
            if (eventDB == null)
            {
                Debug.LogError($"[EventDataLoader] EventDB를 찾을 수 없습니다: {dbPath}");
                return;
            }

            if (eventDB.DeleteDB())
            {
                EditorUtility.SetDirty(eventDB);
                AssetDatabase.SaveAssets();
                Debug.Log("[EventDataLoader] 이벤트 DB 삭제");
            }
        }
    }
}
#endif
