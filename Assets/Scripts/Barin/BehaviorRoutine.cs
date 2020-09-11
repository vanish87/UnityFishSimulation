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

        [SerializeField] protected List<AnimationCurve> curves;
        [SerializeField] protected TuningData swimming;
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
                    this.swimming = this.smc.ActivationData.Tuning;
                    this.curves = this.smc.ActivationData.ToAnimationCurves();
                }
                this.motorControllers.Add(this.smc);
            }
            else if (motorType == Focusser.MotorPreference.Type.TurnRight)
            {
                var mc = new TurnMC();
                this.motorControllers.Add(mc);
            }
        }
    }
}

