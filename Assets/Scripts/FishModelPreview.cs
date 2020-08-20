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

        [SerializeField] protected List<float3> traj = new List<float3>();

        [SerializeField] protected FishSimulator simulator;
        protected FishActivationData activationData;
        
        protected void InitActivations()
        {
            this.activationData = new FishActivationData(this.timeInterval, this.sampleNum);

            foreach (var fun in this.activationData.Activations.Values)
            {
                fun.RandomValues();
            }
        }

        protected void UpdateAnimations()
        {
            this.curves.Clear();
            foreach(var func in this.activationData.Activations.Values)
            {
                this.curves.Add(func.ToAnimationCurve());
            }
        }

        protected void UpdateAnimationsFunctions()
        {
            this.activationData.Activations[Spring.Type.MuscleMiddle] = new X2FDiscreteFunction<float>(this.curves[0]);
            this.activationData.Activations[Spring.Type.MuscleBack] = new X2FDiscreteFunction<float>(this.curves[1]);
        }

        protected void UpdateTraj()
        {
            this.traj.Clear();
            var sol = this.simulator.CurrentSolution as FishSimulator.Solution;
            this.traj.AddRange(sol.trajactory.ToYVector());
        }

        protected void Start()
        {
            this.InitActivations();
            this.UpdateAnimations();

            var problem = new FishSimulator.Problem(this.activationData.Activations);
            var delta = new FishSimulator.Delta();

            this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta, this.stepMode);
            this.simulator.StartSimulation();
        }

        protected void Update()
        {
            if (this.simulator.IsSimulationDone())
            {
                this.UpdateTraj();

                this.UpdateAnimationsFunctions();
                this.simulator.StartSimulation();
            }

            if (this.mode == ControlMode.Manual)
            {
                foreach (var a in this.activationData.Activations.Values)
                {
                    foreach (var i in System.Linq.Enumerable.Range(0, this.sampleNum))
                    {
                        a[i] = this.activation;
                    }
                }
            }

            this.simulator.RunMode = this.stepMode;

            if (Input.GetKey(KeyCode.S))
            {
                this.simulator.TryToRun();
            }
        }

        protected void OnDisable()
        {
            this.simulator.StopThread();
        }

        protected void OnDrawGizmos()
        {
            using (new GizmosScope(Color.white, this.transform.localToWorldMatrix))
            {
                this.simulator?.OnGizmos();
                
                for (var i = 0; i < this.traj.Count - 1; ++i)
                {
                    Gizmos.DrawLine(this.traj[i], this.traj[i + 1]);
                }
            }
        }

    }
}

