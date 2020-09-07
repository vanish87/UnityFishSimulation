using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{
    public static class Matrix4x4e
    {
        public static readonly Matrix4x4 identity = Matrix4x4.identity;
    }
    public static class float3e
    {
        public static readonly float3 one = new float3(1, 1, 1);
    }
    
    public class VisionSensor : MonoBehaviour, ISensorableObject, FishManager.IManagerUser
    {
        [SerializeField] protected bool debug = false;
        [SerializeField] protected float size = 1;//vision size
        [SerializeField] protected int angle = 300;
        [SerializeField] protected float Vr = 1; // visual range
        [SerializeField] protected float Or = 2; //foraging range
        [SerializeField] protected float Dr = 0.1f; //danger range
        
        public float3 Position => this.transform.position;
        public float Size => this.size;

        public FishManager Manager { get; set; }

        public Type ObjType => Type.Predator;

        public float GetDistance(ISensorableObject other)
        {
            return math.distance(this.Position, other.Position) * other.Size;
        }

        public void Scan(SensorData sensorData)
        {
            sensorData.currentSensorableObjects.Clear();
            sensorData.currentVisiableObjects.Clear();
            sensorData.currentDangerObjects.Clear();
            sensorData.closestObject = null;
            sensorData.closestDistance = -1;

            var from = this as ISensorableObject;
            var o = new float3(this.Position);
            var dir = new float3(this.transform.forward);
            var minDis = this.Vr;
            LogTool.AssertIsTrue(this.Manager != null);
            foreach (var f in this.Manager.SensorableObjects)
            {
                if (from == f) continue;
                var distance = from.GetDistance(f);
                if (distance < this.Or)
                {
                    sensorData.currentSensorableObjects.Add(f);

                    var p = f.Position;
                    var dis = math.length(p - o);
                    var angle = math.dot(math.normalize(p - o), math.normalize(dir));
                    if (dis < this.Vr && angle > math.cos(math.radians(this.angle / 2)))
                    {
                        sensorData.currentVisiableObjects.Add(f);

                        if(distance < this.Dr)
                        {
                            sensorData.currentDangerObjects.Add(f);
                            if(distance < minDis)
                            {
                                minDis = distance;
                                sensorData.closestObject = f;
                            }
                        }
                    }
                }
            }
        }

        protected void OnDrawGizmos()
        {
            if (this.debug)
            {
                using (new GizmosScope(Color.white, Matrix4x4.identity))
                {
                    Gizmos.DrawWireSphere(this.Position, this.Vr);
                    Gizmos.DrawWireSphere(this.Position, this.Or);
                }
            }
        }

    }   
    

    

    
}