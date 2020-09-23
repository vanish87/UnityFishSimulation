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
                var up = this.modelData.Normal;
                var forward = this.modelData.Direction;
                var left = UnityTools.Math.Tool.Normal(up, forward);
                // parent.transform.position = this.modelData.GeometryCenter;
                // parent.transform.up = up;
                // parent.transform.forward = left;
                // parent.transform.right = UnityTools.Math.Tool.Normal(left, up);
            }
        }
        protected void OnDrawGizmos()
        {
            this.modelData?.OnGizmos(GeometryFunctions.springColorMap);
        }
    }
}
