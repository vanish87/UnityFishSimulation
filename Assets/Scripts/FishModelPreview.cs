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
        [SerializeField, Range(-1, 1)] protected float activation = 0f;
        [SerializeField] protected float2 timeInterval = new float2(0, 20);
        [SerializeField] protected int sampleNum = 15;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();


        protected FishActivationData activationData;
        protected FishBody fish;
        protected FishSimulatorOffline sim;
        
        protected void InitActivations()
        {
            this.activationData = new FishActivationDataSwimming(this.timeInterval, this.sampleNum);
            this.activationData.RandomActivation();
       }

        protected void UpdateAnimations()
        {
            this.curves = this.activationData.ToAnimationCurves();
        }

        protected void UpdateAnimationsFunctions()
        {
        }

        protected void Start()
        {
            this.fish = this.GetComponent<FishBody>();
            this.fish.Init();

            this.InitActivations();
            this.UpdateAnimations();
            var problem = new FishSimulatorOffline.Problem(this.fish.modelData, this.activationData);
            var dt = new IterationDelta();
            this.sim = new FishSimulatorOffline(problem, dt);
            this.sim.TryToRun();
        }

        protected void Update()
        {
            if (this.mode == ControlMode.Manual)
            {
                var types = new List<Spring.Type>(){Spring.Type.MuscleMiddle, Spring.Type.MuscleBack};
                foreach (var t in types)
                {
                    var a = this.activationData[t, Spring.Side.Left];
                    a.Tuning.useFFT = false;
                    a.DiscreteFunction.ResetValues(this.activation);
                    a = this.activationData[t, Spring.Side.Right];
                    a.Tuning.useFFT = false;
                    var ra = (this.activation + 1) * 0.5f;
                    ra = 1 - ra;
                    ra = ra * 2 - 1;
                    a.DiscreteFunction.ResetValues(ra);
                }
            }

            

            if (Input.GetKey(KeyCode.S))
            {
                this.sim.TryToRun();
            }
            if(Input.GetKeyDown(KeyCode.R))
            {
                this.UpdateAnimationsFunctions();
                this.sim.TryToRun();
            }

            if(Input.GetKeyDown(KeyCode.G))
            {
                {
                    this.activationData.RandomActivation();
                }
                this.UpdateAnimations();
            }
        }

        protected void OnDrawGizmos()
        {
            var sol = this.sim?.CurrentSolution as FishSimulatorOffline.Solution;
            using (new GizmosScope(Color.white, this.transform.localToWorldMatrix))
            {
                if(sol != null)
                {
                    var logPos = sol.logger.LogData.trajectory.ToYVector();
                    for(var i = 0; i < logPos.Size-1; ++i)
                    {
                        Gizmos.DrawLine(logPos[i], logPos[i + 1]);
                    }
                }
            }
        }

    }
}

