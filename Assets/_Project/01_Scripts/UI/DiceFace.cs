using UnityEngine;

namespace OzGameLab01.UI
{
    public class DiceFace : MonoBehaviour
    {
        // Index layout: 0=TL, 1=TR, 2=ML, 3=Center, 4=MR, 5=BL, 6=BR
        [SerializeField] private GameObject[] dots = new GameObject[7];

        private static readonly int[][] FacePatterns = new int[][]
        {
            new int[] { 3 },
            new int[] { 0, 6 },
            new int[] { 0, 3, 6 },
            new int[] { 0, 1, 5, 6 },
            new int[] { 0, 1, 3, 5, 6 },
            new int[] { 0, 1, 2, 4, 5, 6 },
        };

        public void SetFace(int value)
        {
            int clamped = Mathf.Clamp(value, 1, 6);
            int[] pattern = FacePatterns[clamped - 1];

            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null)
                {
                    continue;
                }

                dots[i].SetActive(System.Array.IndexOf(pattern, i) >= 0);
            }
        }
    }
}
