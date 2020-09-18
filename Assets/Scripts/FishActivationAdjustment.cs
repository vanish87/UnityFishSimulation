using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Attributes;

namespace UnityFishSimulation
{
    public class FishActivationAdjustment : MonoBehaviour
    {
        public enum MCType
        {
            Swim,
            TurnLeft,
            TurnRight,
        }
        [SerializeField] protected MCType type = MCType.Swim;
        [SerializeField] protected bool fft = false;
        [SerializeField] protected bool loop = true;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
        [SerializeField] protected List<TuningData> tunings = new List<TuningData>();

        protected FishActivationData Activation => this.GetCurrentMC().ActivationData;
        [SerializeField] protected SwimMC smc;
        [SerializeField] protected TurnLeftMC lmc;
        [SerializeField] protected TurnRightMC rmc;
        protected FishBody fish;
        protected FishSimulatorOffline sim;
        protected FishSimulatorOffline.Problem problem;
        protected IterationDelta delta;

        protected void Start()
        {
            this.fish = this.GetComponent<FishBody>();
            this.fish.Init();

            this.InitActivations();
            this.UpdateAnimationCurves();
            this.problem = new FishSimulatorOffline.Problem(this.fish, this.GetCurrentMC());
            this.delta = new IterationDelta();
            this.sim = new FishSimulatorOffline(problem, this.delta);
            this.sim.TryToRun();
        }
        protected void Update()
        {
            foreach(var a in this.Activation.ToActivationList())
            {
                a.Tuning.useFFT = this.fft;
            }
            if(this.loop && (this.sim.CurrentSolution as FishSimulatorOffline.Solution).IsDone)
            {
                this.sim.Restart();
            }
            if(Input.GetKeyDown(KeyCode.S))
            {
                FishActivationData.Save(this.Activation);
            }
        }

        protected MuscleMC GetCurrentMC()
        {
            switch (this.type)
            {
                case MCType.Swim: return this.smc;
                case MCType.TurnLeft: return this.lmc;
                case MCType.TurnRight: return this.rmc;
                default: return this.smc;
            }
        }
        protected void InitActivations()
        {
            this.smc = new SwimMC();
            this.lmc = new TurnLeftMC();
            this.rmc = new TurnRightMC();
        }

        protected void UpdateAnimationCurves()
        {
            this.curves = this.Activation.ToAnimationCurves();
            this.tunings = this.Activation.ToActivationList().Select(a=>a.Tuning).ToList();
        }

        protected void UpdateActivationDataFromCurves()
        {
            var act = this.Activation.ToActivationList();
            for (var i = 0; i < this.curves.Count; i += 2)
            {
                act[i / 2].FromAnimationCurve(this.curves[i]);
            }
            this.UpdateAnimationCurves();
        }
    }
}
