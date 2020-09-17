using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace UnityFishSimulation
{
    [System.Serializable]
    public class BehaviorRoutine
    {
        protected List<MotorController> motorControllers = new List<MotorController>();

        protected SwimMC smc;
        protected TurnMC tmc;

        [SerializeField] protected List<AnimationCurve> curves;
        [SerializeField] protected TuningData tuning;
        public List<MotorController> ToMC()
        {
            return this.motorControllers;
        }
        public void Init(Intension intension, Perception perception)
        {
            this.motorControllers.Clear();

            var focusser = perception.GetFocuser();

            var motorType = focusser.motorPreference.MaxValue.type;
            if (motorType == Focusser.MotorPreference.Type.MoveForward)
            {
                if(this.smc == null)
                {
                    this.smc = this.smc ?? new SwimMC();
                    this.curves = this.smc.ActivationData.ToAnimationCurves();
                }
                if(focusser.target.obj != null)
                {
                    this.smc.UpdateSpeed(focusser.target.obj.distance);
                }
                this.motorControllers.Add(this.smc);
            }
            else if (motorType == Focusser.MotorPreference.Type.TurnRight)
            {
                if(this.tmc == null)
                {
                    this.tmc = new TurnMC();
                    this.curves = this.tmc.ActivationData.ToAnimationCurves();
                }
                this.motorControllers.Add(this.tmc);
            }
        }

        protected float GetAngleInFish(float3 targetDirection, float3 normal, float3 left)
        {
            normal = math.normalize(normal);
            var projection = targetDirection - (math.dot(targetDirection, normal) * normal);
            var angle = math.dot(projection, left);
            return angle;
            // var theta = math.PI/5;
            // var forward = math.PI/2;
            // //>0 left
            // //<0 right
            // var forwardAngle = new float2(math.cos(forward - theta), math.cos(forward + theta));
            // if(forwardAngle.x > angle && angle > forwardAngle.y) return MotorPreference.Type.MoveForward;
            // return angle > 0? MotorPreference.Type.TurnLeft:MotorPreference.Type.TurnRight;
        }
        
         
    }
}

