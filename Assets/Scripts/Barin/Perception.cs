using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    [System.Serializable]
    public class SensorData
    {
        [SerializeField] public float worldBrightness = 1;

        [SerializeField] public List<ISensorableObject> currentSensorableObjects = new List<ISensorableObject>();
        [SerializeField] public List<ISensorableObject> currentVisiableObjects = new List<ISensorableObject>();
        [SerializeField] public List<ISensorableObject> currentDangerObjects = new List<ISensorableObject>();

        [SerializeField] public ISensorableObject closestObject;
        [SerializeField] public float closestDistance = -1;
    }
    public class Perception : MonoBehaviour
    {
        protected VisionSensor visionSensor;
        protected TemperatureSensor temperatureSensor;

        private SensorData sensorData = new SensorData();

        public Focusser focusser = new Focusser();

        public void Init()
        {
            this.visionSensor = this.GetComponentInChildren<VisionSensor>();
            this.temperatureSensor = this.GetComponentInChildren<TemperatureSensor>();
        }

        public void SensorUpdate(float t)
        {
            this.visionSensor.Scan(this.sensorData);
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
                MoveForawrd
            }
        }

        public Target target;
        //active focus and filter out none-important sensor data
        //save to Target
        public Target Update(Intension intension, Perception perception, MentalState mental)
        {
            //calculate desires
            //avoid, fear, eat, mate


            //if intension == avoid

            //if intension == escape

            return default;
        }

        protected MotorPreference IntensionToMotoPerference(Intension intension)
        {

            return default;
        }
    }
}
