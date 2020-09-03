using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Debuging.EditorTool;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishModelPreview : MonoBehaviour
    {
        public enum ControlMode
        {
            Manual,
            Activations,
        }

        [SerializeField] protected ControlMode mode = ControlMode.Activations;
        [SerializeField] protected IterationAlgorithmMode stepMode = IterationAlgorithmMode.FullStep;
        [SerializeField, Range(0, 1)] protected float activation = 0.5f;
        [SerializeField] protected float2 timeInterval = new float2(0, 20);
        [SerializeField] protected int sampleNum = 15;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
        [SerializeField] protected TuningData tuning;

        [SerializeField] protected List<float3> traj = new List<float3>();

        protected FishActivationData activationData;
        protected Fish fish;
        
        protected void InitActivations()
        {
            this.activationData = new FishActivationDataSwimming(this.timeInterval, this.sampleNum);

            foreach (var fun in this.activationData.ToDiscreteFunctions())
            {
                fun.RandomValues();
            }

            this.activationData.GenerateFFTData();
            this.tuning = this.activationData.Tuning;
            this.tuning.useFFT = false;
        }

        protected void UpdateAnimations()
        {
            this.curves = this.activationData.ToAnimationCurves();
        }

        protected void UpdateAnimationsFunctions()
        {
            this.activationData[Spring.Type.MuscleMiddle] = new X2FDiscreteFunction<float>(this.curves[0]);
            this.activationData[Spring.Type.MuscleBack] = new X2FDiscreteFunction<float>(this.curves[1]);
        }

        /*protected void UpdateTraj()
        {
            this.traj.Clear();
            var sol = this.simulator.CurrentSolution as FishSimulator.Solution;
            this.traj.AddRange(sol.trajactory.ToYVector());
        }*/

        protected void Start()
        {
            this.fish = this.GetComponent<Fish>();

            this.InitActivations();
            this.UpdateAnimations();

            this.fish.Brain.temp = this.activationData;

            this.fish.sim.TryToRun();
        }

        protected void Update()
        {
            if (this.mode == ControlMode.Manual)
            {
                foreach (var a in this.activationData.ToDiscreteFunctions())
                {
                    foreach (var i in System.Linq.Enumerable.Range(0, this.sampleNum))
                    {
                        a[i] = this.activation;
                    }
                }
            }

            fish.sim.RunMode = this.stepMode;

            if (Input.GetKey(KeyCode.S))
            {
                fish.sim.TryToRun();
            }
            if(Input.GetKeyDown(KeyCode.R))
            {
                this.UpdateAnimationsFunctions();
                fish.sim.ResetAndRun();
            }

            if(Input.GetKeyDown(KeyCode.G))
            {
                foreach (var fun in this.activationData.ToDiscreteFunctions())
                {
                    fun.RandomValues();
                }
                this.UpdateAnimations();
            }
        }

        protected void OnDrawGizmos()
        {
            using (new GizmosScope(Color.white, this.transform.localToWorldMatrix))
            {
                for (var i = 0; i < this.traj.Count - 1; ++i)
                {
                    Gizmos.DrawLine(this.traj[i], this.traj[i + 1]);
                }
            }
        }

    }
}

