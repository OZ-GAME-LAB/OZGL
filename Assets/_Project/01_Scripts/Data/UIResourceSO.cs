using UnityEngine;

namespace OzGameLab01.UI
{
    [CreateAssetMenu(fileName = "UIResourceSO",menuName = "OZGL/UI/UI Resource SO")]
    public sealed class UIResourceSO : ScriptableObject
    {
        [Tooltip("Title UI 전용 리소스")]
        public TitleUiSO titleUi;
    }
}