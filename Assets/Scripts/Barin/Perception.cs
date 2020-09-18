using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{
    public class SensorObject
    {
        public ISensorableObject obj;
        public float distance;
    }

    [System.Serializable]
    public class SensorData
    {
        
        [SerializeField] public float worldBrightness = 1;

        [SerializeField] protected Dictionary<ObjectType, List<SensorObject>> currentSensorableObjects = new Dictionary<ObjectType, List<SensorObject>>();
        [SerializeField] protected Dictionary<ObjectType, List<SensorObject>> currentVisiableObjects = new Dictionary<ObjectType, List<SensorObject>>(); 
        [SerializeField] protected Dictionary<ObjectType, List<SensorObject>> currentDangerObjects = new Dictionary<ObjectType, List<SensorObject>>();

        [SerializeField] protected Dictionary<ObjectType, SensorObject> closestObjects = new Dictionary<ObjectType, SensorObject>();
        [SerializeField] public SensorObject closestDangerObj = null;

        public SensorData()
        {
            foreach (ObjectType t in Enum.GetValues(typeof(ObjectType)))
            {
                this.currentSensorableObjects.Add(t, new List<SensorObject>());
                this.currentVisiableObjects.Add(t, new List<SensorObject>());
                this.currentDangerObjects.Add(t, new List<SensorObject>());
            }
        }
        public void SetClosest(ISensorableObject obj, float distance)
        {
            var type = obj.ObjType;
            var newObj = new SensorObject() { obj = obj, distance = distance };
            if(this.closestObjects.ContainsKey(type))
            {
                this.closestObjects[type] = newObj;    
            }
            else
            {
                this.closestObjects.Add(type, newObj);
            }
        }
        public SensorObject GetClosestByType(ObjectType type)
        {
            return this.closestObjects.ContainsKey(type) ? this.closestObjects[type] : null;
        }

        public List<SensorObject> GetSensorable(ObjectType type) { return this.currentSensorableObjects[type]; }
        public List<SensorObject> GetVisiable(ObjectType type) { return this.currentVisiableObjects[type]; }
        public List<SensorObject> GetDanger(ObjectType type) { return this.currentDangerObjects[type]; }
        public void AddSensorable(ISensorableObject obj, float distance)
        {
            var type = obj.ObjType;
            this.currentSensorableObjects[type].Add(new SensorObject(){obj = obj, distance = distance});
        }
        public void AddVisiable(ISensorableObject obj, float distance)
        {
            var type = obj.ObjType;
            this.currentVisiableObjects[type].Add(new SensorObject(){obj = obj, distance = distance});
        }
        public void AddDanger(ISensorableObject obj, float distance)
        {
            var type = obj.ObjType;
            this.currentDangerObjects[type].Add(new SensorObject(){obj = obj, distance = distance});
        }
        public void Clear()
        {
            foreach(var l in this.currentSensorableObjects.Values) l.Clear();
            foreach(var l in this.currentVisiableObjects.Values) l.Clear();
            foreach(var l in this.currentDangerObjects.Values) l.Clear();
            
            this.closestObjects.Clear();
            this.closestDangerObj = null;
        }
    }
    public class Perception : MonoBehaviour
    {
        protected VisionSensor visionSensor;
        protected TemperatureSensor temperatureSensor;

        private SensorData sensorData = new SensorData();

        [SerializeField] protected Focusser focusser = new Focusser();

        public void Init()
        {
            this.visionSensor = this.GetComponent<VisionSensor>();
            this.temperatureSensor = this.GetComponent<TemperatureSensor>();
        }

        public void SensorUpdate(FishSimulator.Delta delta)
        {
            this.sensorData.Clear();
            this.visionSensor.Scan(this.sensorData);
        }
        public void FocusserUpdate(Intension intension, Desire desire)
        {
            this.focusser.Update(intension, this, desire);
            //TODO refine this
            this.focusser.target.self = this.gameObject;
        }
        public Focusser GetFocuser(){return this.focusser;}

        public SensorData GetSensorData() { return this.sensorData; }


        protected void OnDrawGizmos()
        {
            this.focusser.OnDrawGizmos();
        }
    }

    [System.Serializable]
    public class Focusser
    {
        
        [System.Serializable]
        public class Target
        {
            public SensorObject obj;
            public GameObject self;
        }
        [System.Serializable]
        //Intension->MotorPreference->Focus
        public class MotorPreference
        {
            public enum Type
            {
                TurnLeft,
                TurnRight,
                MoveForward,
                MoveBackward,
                Ascend,
                Descend,
            }
            [Serializable]
            public class Data
            {
                public Type type;
                public float value;
            }
            [SerializeField] protected List<Data> motorPreferenceData = new List<Data>();

            public Data MaxValue=>this.motorPreferenceData.OrderByDescending(m=>m.value).FirstOrDefault();
            public Data this[Type t]
            {
                get => this.motorPreferenceData.Where(m=>m.type == t).FirstOrDefault();
            }
            public MotorPreference()
            {
                foreach(Type t in Enum.GetValues(typeof(Type)))
                {
                    this.motorPreferenceData.Add(new Data(){type = t, value = 0});
                }
            }
            public void Clear()
            {
                foreach(var v in this.motorPreferenceData)
                {
                    v.value = 0;
                }
            }
        }

        [SerializeField] public MotorPreference motorPreference = new MotorPreference();
        public Target target = new Target();
        //active focus and filter out none-important sensor data
        //save to Target
        public void Update(Intension intension, Perception perception, Desire desire)
        {
            //get desires
            //avoid, fear, eat, mate

            this.motorPreference.Clear();
            var foods = perception.GetSensorData().GetVisiable(ObjectType.Food);
            foreach (var f in foods)
            {
                var mtype = this.GetMotorPreferenceType(f.obj, perception);
                this.motorPreference[mtype].value += desire.eat;
            }

            var obstacles = perception.GetSensorData().GetVisiable(ObjectType.Obstacle);
            foreach(var o in obstacles)
            {
                var mtype = this.GetMotorPreferenceType(o.obj, perception);
                this.motorPreference[mtype].value -= desire.avoid;
            }
            //TODO add predator value

            var targetType = intension.IntensionType == Intension.Type.Eat ? ObjectType.Food : ObjectType.Obstacle;

            var sameSide = foods.Where(o=>this.GetMotorPreferenceType(o.obj, perception) == this.motorPreference.MaxValue.type);
            this.target.obj = sameSide.OrderBy(o=>o.distance).FirstOrDefault();
            //if intension == avoid

            //if intension == escape

        }

        //TODO return multiple motorpreference
        protected MotorPreference.Type GetMotorPreferenceType(ISensorableObject obj, Perception perception)
        {
            var org = perception.transform.position;
            var dir = perception.transform.forward; // Note forward is blue axis

            var target = math.normalize(obj.Position - new float3(org));
            var angle = math.dot(target, dir);
            //>0 left
            //<0 right
            var forwardAngle = new float2(math.cos(math.radians(90-25)), math.cos(math.radians(90+25)));
            if(forwardAngle.x > angle && angle > forwardAngle.y) return MotorPreference.Type.MoveForward;
            return angle > 0? MotorPreference.Type.TurnLeft:MotorPreference.Type.TurnRight;
        }

        public void OnDrawGizmos()
        {
            if(this.target.obj != null)
            {
                using(new GizmosScope(Color.red, Matrix4x4.identity))
                {
                    Gizmos.DrawSphere(target.obj.obj.Position, 2);
                }
            }
        }
    }
}
