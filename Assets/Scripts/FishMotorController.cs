using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Math;
using static UnityFishSimulation.FishSAOptimizer.FishSA;

namespace UnityFishSimulation
{
    /*[Serializable]
    public class FishMotorController
    {
        protected const float Smax = 0.075f;

        [SerializeField]
        protected List<float2> amplitudeParameter = new List<float2>(2)
        {
            new float2(0,1),
            new float2(0,1),
        };
        [SerializeField]
        protected List<float2> frequencyParameter = new List<float2>(2)
        {
            new float2(0,Smax),
            new float2(0,Smax),
        };
    }*/

    [Serializable]
    public class FishSwimmingMC : FishSimulator.Problem
    {
        [SerializeField] protected float speed = 1;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
        [SerializeField] protected List<float> offset = new List<float>();
        [SerializeField] protected List<TuningData> tuningData;

        public FishSwimmingMC() : base()
        {
            //FishActivationData.UpdateFFT(this.Current.Activations, 1, false);
            

            /*foreach (var r in Enumerable.Range(0, 10))
            {
                var current = FishActivationData.Load();
                //FishActivationData.UpdateFFT(current.Activations, 1, false);
                this.fishActivationDatas.Enqueue(current);
            }
            var otherAct = FishActivationData.Load("Swimming2");
            //FishActivationData.UpdateFFT(otherAct.Activations, 1, false);
            this.fishActivationDatas.Enqueue(otherAct);*/

            foreach (var act in this.fishActivationDatas) act.GenerateFFTData();
            this.tuningData = this.fishActivationDatas.Select(d => d.Tuning).ToList();

            this.curves.Clear();
            this.curves.AddRange(this.Current.ToAnimationCurves());
        }

        public override void ReloadData()
        {
            this.fish = this.fish ?? GeometryFunctions.Load();
            this.curves.Clear();
            this.curves.AddRange(this.Current.ToAnimationCurves());
        }
    }

    /*[Serializable]
    public class FishTurnMC : FishMotorController
    {
        //left is negative
        //right is positive
        [SerializeField] protected int angle = 0;
    }*/
}
