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
        protected void Update()
        {
            var parent = this.transform.parent;
            if (parent != null)
            {
                parent.transform.position = this.modelData.GeometryCenter;
                parent.transform.forward = this.modelData.Left;
                parent.transform.right = this.modelData.Direction;
                // this.transform.up = this.modelData.Normal;
            }
        }
        protected void OnDrawGizmos()
        {
            this.modelData?.OnGizmos(GeometryFunctions.springColorMap);
        }
    }
}
