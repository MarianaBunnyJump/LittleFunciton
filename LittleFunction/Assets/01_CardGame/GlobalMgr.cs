#region

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

#endregion

public class GlobalMgr : MonoBehaviour
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] [ReadOnly] private int nowCount;

    public void AddCount(int getCount)
    {
        nowCount += getCount;
        countText.text = nowCount.ToString();
    }
}