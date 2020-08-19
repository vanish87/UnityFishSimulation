using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishModelPreview : MonoBehaviour
    {
        public enum Mode
        {
            Manual,
            Activations,
        }

        [SerializeField] protected Mode mode = Mode.Activations;
        [SerializeField, Range(0, 1)] protected float activation = 0.5f;
        [SerializeField] protected float2 timeInterval = new float2(0, 20);
        [SerializeField] protected int sampleNum = 15;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();

        [SerializeField] protected List<float3> traj = new List<float3>();

        protected FishSimulator simulator;
        protected Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();
        
        protected void InitActivations()
        {
            this.activations.Clear();

            var start = new Tuple<float, float>(this.timeInterval.x, 0.5f);
            var end = new Tuple<float, float>(this.timeInterval.y, 0.5f);

            //this.activations.Add(Spring.Type.MuscleFront, new X2FDiscreteFunction<float>(start, end, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleMiddle, new X2FDiscreteFunction<float>(start, end, this.sampleNum));
            this.activations.Add(Spring.Type.MuscleBack, new X2FDiscreteFunction<float>(start, end, this.sampleNum));

            foreach (var fun in this.activations.Values)
            {
                fun.RandomValues();
            }
        }

        protected void UpdateAnimations()
        {
            this.curves.Clear();
            foreach(var func in this.activations.Values)
            {
                this.curves.Add(func.ToAnimationCurve());
            }
        }

        protected void UpdateAnimationsFunctions()
        {
            this.activations[Spring.Type.MuscleMiddle] = new X2FDiscreteFunction<float>(this.curves[0]);
            this.activations[Spring.Type.MuscleBack] = new X2FDiscreteFunction<float>(this.curves[1]);
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

            var problem = new FishSimulator.Problem(this.activations);
            var delta = new FishSimulator.Delta();

            this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
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


            if (this.mode == Mode.Manual)
            {
                foreach (var a in this.activations.Values)
                {
                    foreach (var i in System.Linq.Enumerable.Range(0, this.sampleNum))
                    {
                        a[i] = this.activation;
                    }
                }
            }
        }

        protected void OnDisable()
        {
            this.simulator.StopThread();
        }

        protected void OnDrawGizmos()
        {
            this.simulator?.OnGizmos();

            for(var i = 0; i < this.traj.Count-1; ++i)
            {
                Gizmos.DrawLine(this.traj[i], this.traj[i + 1]);
            }
        }

    }
}

