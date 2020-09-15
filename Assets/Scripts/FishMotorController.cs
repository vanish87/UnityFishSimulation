using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace UnityFishSimulation
{
    /*[Serializable]
    public class FishSwimmingMC : FishSimulator.Problem
    {
        [SerializeField] protected float speed = 1;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
        [SerializeField] protected List<TuningData> tuningData;

        public FishSwimmingMC() : base()
        {
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

    [Serializable]
    public class FishTurnMC : FishSimulator.Problem
    {
        //left is negative
        //right is positive
        [SerializeField] protected int angle = 0;

        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
        [SerializeField] protected List<TuningData> tuningData;

        public FishTurnMC() : base(FishActivationData.Load("TurnRight"))
        {
            this.tuningData = this.fishActivationDatas.Select(d => d.Tuning).ToList();
            this.Current.GenerateFFTData();
        }

        public override void ReloadData()
        {
            this.fish = this.fish ?? GeometryFunctions.Load();
            this.curves.Clear();
            this.curves.AddRange(this.Current.ToAnimationCurves());
        }
    }*/
}
