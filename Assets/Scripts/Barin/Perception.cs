using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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

        protected Focusser focusser = new Focusser();

        public void Init()
        {
            this.visionSensor = this.GetComponentInChildren<VisionSensor>();
            this.temperatureSensor = this.GetComponentInChildren<TemperatureSensor>();
        }

        public void SensorUpdate(float t)
        {
            this.visionSensor.Scan(this.sensorData);
        }
        public void UpdateFocusser(Intension intension, MentalState mental)
        {
            this.focusser.Update(intension, this, mental);
        }

        public SensorData GetSensorData() { return this.sensorData; }
        public Focusser.CollisionInfo GetCollisions() { return this.focusser.target.collisionInfo; }
    }
    [System.Serializable]

    public class Focusser
    {
        [System.Serializable]
        public class CollisionInfo
        {
            public GameObject closesObj;
            public List<GameObject> collisions = new List<GameObject>();
        }
        [System.Serializable]
        public class Target
        {
            public float3 position;

            public CollisionInfo collisionInfo;
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
            public float value;
        }

        public Dictionary<MotorPreference, float> motorPreferenceData = new Dictionary<MotorPreference, float>();
        public Target target;
        //active focus and filter out none-important sensor data
        //save to Target
        public Target Update(Intension intension, Perception perception, MentalState mental)
        {
            //calculated desires
            //avoid, fear, eat, mate


            //if intension == avoid

            //if intension == escape

            return default;
        }

        protected MotorPreference IntensionToMotorPreference(Intension intension)
        {

            return default;
        }
    }
}
