#region

using UnityEngine;

#endregion


    [CreateAssetMenu(fileName = "CurveParameters_1", menuName = "MyCreate/CurveParameters")]
    public class CurveParameters : ScriptableObject
    {
        public AnimationCurve positioning;
        public float positioningInfluence = .1f;
        public AnimationCurve rotation;
        public float rotationInfluence = 10f;
    }
