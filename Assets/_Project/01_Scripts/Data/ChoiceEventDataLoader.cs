#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class ChoiceEventDataLoader
{
    [MenuItem("Tools/DB/DataLoadChoiceEvent")]
    public static void DataLoad()
    {
        string dbPath = "Assets/_Project/05_Data/ChoiceEvent/ChoiceEventDB.asset";

        ChoiceEventDB choiceEventDB = AssetDatabase.LoadAssetAtPath<ChoiceEventDB>(dbPath);

        string[] guids = AssetDatabase.FindAssets("t:ChoiceEventSO", new[] { "Assets/_Project/05_Data/ChoiceEvent" }); //Assets/_Project/05_Data/ChoiceEvent

        Dictionary<string, ChoiceEventSO> eventList = new Dictionary<string, ChoiceEventSO>();

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

            ChoiceEventSO tempSO = AssetDatabase.LoadAssetAtPath<ChoiceEventSO>(assetPath);

            if (tempSO != null && !eventList.ContainsKey((tempSO.id)))
            {
                eventList.Add(tempSO.id, tempSO);
            }

            if (tempSO.eventCategory == EventCategory.Choice)
            {
                choiceEventDB.EventList_Event.Add(tempSO);
            }

            switch (tempSO.choiceCategory)
            {
                case EventChoiceCategory.Relic:
                    choiceEventDB.EventList_Relic.Add(tempSO);
                    break;
                case EventChoiceCategory.Battle:
                    choiceEventDB.EventList_Battle.Add(tempSO);
                    break;
                case EventChoiceCategory.Quiz:
                    choiceEventDB.EventList_Quiz.Add(tempSO);
                    break;
                case EventChoiceCategory.Exit:
                    choiceEventDB.EventList_Finish.Add(tempSO);
                    break;
                default:
                    break;
            }

            if (choiceEventDB.SetEvent(eventList))
            {
                Debug.Log("[ChoiceEventDataLoader] 이벤트 DB 삽입");
            }
            EditorUtility.SetDirty(choiceEventDB);
            AssetDatabase.SaveAssets();
        }
    }
    [MenuItem("Tools/DB/DataDeleteChoiceEvent")]
    public static void DataDelete()
    {
        string dbPath = "Assets/_Project/05_Data/ChoiceEvent/ChoiceEventDB.asset";

        ChoiceEventDB choiceEventDB = AssetDatabase.LoadAssetAtPath<ChoiceEventDB>(dbPath);

        if (choiceEventDB.DeleteDB())
        {
            Debug.Log("[ChoiceEventDataLoader] 이벤트 DB 삭제");
        }
    }
}

#endif