using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishFood : MonoBehaviour, ISensorableObject
    {
        public float3 Position => this.transform.position;

        public float Size => 1;

        public ObjectType ObjType => ObjectType.Food;

        public float GetDistance(ISensorableObject other)
        {
            return math.distance(this.Position, other.Position);
        }
    }
}