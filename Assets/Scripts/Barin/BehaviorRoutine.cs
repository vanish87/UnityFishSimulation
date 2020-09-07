using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace UnityFishSimulation
{
    public abstract class BehaviorRoutine
    {
        public abstract List<MotorController> ToMC();
        public abstract void Init(Intension intension, Perception perception);
    }

    public class ObstacleAvoidance : BehaviorRoutine
    {
        public override void Init(Intension intension, Perception perception)
        {

            //sensory information 
            //motor preferences
            //=> MC and MC parameters
        }

        public override List<MotorController> ToMC()
        {
            return default;
        }
    }

    public class ChasingTarget : BehaviorRoutine
    {
        protected float3 target;
        public override void Init(Intension intension, Perception perception)
        {

            //sensory information 
            //motor preferences
            //=> MC and MC parameters
            //this.target = perception.GetSensorData().closestObject.Position;
        }

        public override List<MotorController> ToMC()
        {
            var ret = new List<MotorController>();
            ret.Add(new SwimMC());
            return ret;
        }
    }

    public class Wandering : BehaviorRoutine
    {
        public override void Init(Intension intension, Perception perception)
        {

        }

        public override List<MotorController> ToMC()
        {
            var ret = new List<MotorController>();
            ret.Add(new SwimMC());
            return ret;
        }
    }
}
