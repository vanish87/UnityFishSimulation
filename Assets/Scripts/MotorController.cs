using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class MotorController : MonoBehaviour
    {
        public enum Type
        {
            Swim,
            Balance,
        }
        public class MuscleActuation
        {
            public List<float2> valueMap;
            public float Evaluate(float t)
            {
                //Temp just return sin value
                return math.sin(t);
            }
        }

        [SerializeField] protected StructureModel fishModel;


    }
}
