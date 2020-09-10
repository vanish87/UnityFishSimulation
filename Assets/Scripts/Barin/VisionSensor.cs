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

        public ObjectType ObjType => ObjectType.Predator;

        public float GetDistance(ISensorableObject other)
        {
            return math.distance(this.Position, other.Position) * other.Size;
        }

        public void Scan(SensorData sensorData)
        {
            sensorData.Clear();

            var from = this as ISensorableObject;
            var o = new float3(this.Position);
            var dir = new float3(this.transform.forward);
            var minDis = new Dictionary<ObjectType, float>();
            foreach(ObjectType t in Enum.GetValues(typeof(ObjectType)))
            {
                minDis.Add(t, this.Or);
            } 

            var minDangerDis = this.Dr;
            
            LogTool.AssertIsTrue(this.Manager != null);
            foreach (var f in this.Manager.SensorableObjects)
            {
                if (from == f) continue;
                var distance = from.GetDistance(f);
                if (distance < this.Or)
                {
                    sensorData.AddSensorable(f, distance);

                    var p = f.Position;
                    var angle = math.dot(math.normalize(p - o), math.normalize(dir));
                    if (distance < this.Vr && angle > math.cos(math.radians(this.angle / 2)))
                    {
                        sensorData.AddVisiable(f, distance);

                        if(distance < this.Dr)
                        {
                            sensorData.AddDanger(f, distance);

                            if(distance < minDangerDis)
                            {
                                minDangerDis = distance;
                                sensorData.closestDangerObj = new SensorObject(){obj=f, distance= distance};
                            }
                        }
                    }
                }
                if (distance < minDis[f.ObjType])
                {
                    minDis[f.ObjType] = distance;
                    sensorData.SetClosest(f, distance);
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