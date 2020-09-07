using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishBody : MonoBehaviour
    {
        public FishModelData modelData;

        public void Init()
        {
            this.modelData = GeometryFunctions.Load();
        }
        protected void OnDrawGizmos()
        {
            this.modelData?.OnGizmos(GeometryFunctions.springColorMap);
        }
    }
}
