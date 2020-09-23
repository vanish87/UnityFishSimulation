using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Debuging;
using UnityTools.Math;

namespace UnityFishSimulation
{
    [System.Serializable]
    public class BehaviorRoutine
    {
        protected List<MotorController> motorControllers = new List<MotorController>();

        protected SwimMC smc;
        protected TurnLeftMC tlmc;
        protected TurnRightMC trmc;

        [SerializeField] protected List<AnimationCurve> curves;
        [SerializeField] protected TuningData tuning;
        public List<MotorController> ToMC()
        {
            return this.motorControllers;
        }
        public void Init(Intension intension, Perception perception, FishBody body)
        {
            this.motorControllers.Clear();

            var focusser = perception.GetFocuser();

            var normal = body.modelData.Normal;
            var left = body.modelData.Left;
            var dir = focusser.target.obj.obj.Position - body.modelData.GeometryCenter;

            var motorType = focusser.motorPreference.MaxValue.type;
            if (motorType == Focusser.MotorPreference.Type.MoveForward)
            {
                if(this.smc == null)
                {
                    this.smc = new SwimMC();
                    this.curves = this.smc.ActivationData.ToAnimationCurves();
                }
                if(focusser.target.obj != null)
                {
                    this.smc.UpdateSpeed(focusser.target.obj.distance);
                }
                this.motorControllers.Add(this.smc);
            }
            else if (motorType == Focusser.MotorPreference.Type.TurnLeft)
            {
                if(this.tlmc == null)
                {
                    this.tlmc = new TurnLeftMC();
                    this.curves = this.tlmc.ActivationData.ToAnimationCurves();
                }
                var angle = this.GetAngleInFish(dir, normal, left);
                this.tlmc.UpdateAngle(angle);
                this.motorControllers.Add(this.tlmc);

                this.smc?.UpdateSpeed(0);
            }
            else if (motorType == Focusser.MotorPreference.Type.TurnRight)
            {
                if(this.trmc == null)
                {
                    this.trmc = new TurnRightMC();
                    this.curves = this.trmc.ActivationData.ToAnimationCurves();
                }
                var angle = this.GetAngleInFish(dir, normal, left);
                this.trmc.UpdateAngle(angle);
                this.motorControllers.Add(this.trmc);
                
                this.smc?.UpdateSpeed(0);
            }


            var balance = new BalanceMC();
            balance.UpdateBalance(left, normal);
            this.motorControllers.Add(balance);
        }

        protected float GetAngleInFish(float3 targetDirection, float3 normal, float3 left)
        {
            var projection = Tool.ProjectionOnPlane(targetDirection, normal);
            var angle = Tool.CosAngle(projection, left);
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

