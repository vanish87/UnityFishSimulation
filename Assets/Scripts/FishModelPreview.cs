using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Attributes;
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
            Random,
            CosSwim,
            CosTrun,
        }

        [SerializeField, FileNamePopup("*.ad")] protected string activationFileName = "Swimming";
        [SerializeField] protected ControlMode mode = ControlMode.Activations;
        [SerializeField] protected IterationAlgorithmMode stepMode = IterationAlgorithmMode.FullStep;
        [SerializeField, Range(-1, 1)] protected float activation = 0f;
        [SerializeField] protected float2 timeInterval = new float2(0, 20);
        [SerializeField] protected int sampleNum = 15;
        [SerializeField] protected bool fft = false;
        [SerializeField] protected bool loop = true;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();


        protected FishActivationData activationData;
        protected FishBody fish;
        protected FishSimulatorOffline sim;
        protected FishSimulatorOffline.Problem problem;

        protected void InitActivations()
        {
            if (this.mode == ControlMode.Random || this.mode == ControlMode.Manual)
            {
                this.activationData = new FishActivationDataSwimming(this.timeInterval, this.sampleNum);
                this.activationData.RandomActivation();
            }
            else
            if (this.mode == ControlMode.Activations)
            {
                this.activationData = FishActivationData.Load(this.activationFileName);
            }
            else
            if(this.mode == ControlMode.CosSwim)
            {
                this.activationData = new FishActivationDataSwimming(this.timeInterval, this.sampleNum, true);
                var ml = this.activationData[Spring.Type.MuscleMiddle, Spring.Side.Left];
                var bl = this.activationData[Spring.Type.MuscleBack, Spring.Side.Left];
                for(var i = 0; i < this.sampleNum;++i)
                {
                    var x = i * 1f / (this.sampleNum - 1) * 2 * math.PI;
                    ml.DiscreteFunction[i] = math.sin(x);
                    bl.DiscreteFunction[i] = math.sin(x + math.PI);
                }
                ml.FFT.GenerateFFTData();
                bl.FFT.GenerateFFTData();
            }
            else
            if(this.mode == ControlMode.CosTrun)
            {
                var ar = FishActivationData.Load("TurnRight");

                this.activationData = new FishActivationDataTurnLeft(this.timeInterval, this.sampleNum, true);
                var fl = this.activationData[Spring.Type.MuscleFront, Spring.Side.Left];
                var ml = this.activationData[Spring.Type.MuscleMiddle, Spring.Side.Left];
                var afl = ar[Spring.Type.MuscleFront, Spring.Side.Left];
                var aml = ar[Spring.Type.MuscleMiddle, Spring.Side.Left];
                for(var i = 1; i < this.sampleNum;++i)
                {
                    var x = i * 1f / (this.sampleNum - 1) * 2 * math.PI;
                    if(i < this.sampleNum/3)
                    {
                        // ml.DiscreteFunction[i] = -math.sin(x*3);
                        // fl.DiscreteFunction[i] = -math.sin(x*3);
                    }
                    fl.DiscreteFunction[i] = -afl.DiscreteFunction[i];
                    ml.DiscreteFunction[i] = -aml.DiscreteFunction[i];
                }
                ml.FFT.GenerateFFTData();
                fl.FFT.GenerateFFTData();
 
            }
        }

        protected void UpdateAnimationCurves()
        {
            this.curves = this.activationData.ToAnimationCurves();
        }

        protected void UpdateActivationDataFromCurves()
        {
            var act = this.activationData.ToActivationList();
            for(var i = 0; i < this.curves.Count; i+=2)
            {
                act[i/2].FromAnimationCurve(this.curves[i]);
            }
            this.UpdateAnimationCurves();
        }

        protected void Start()
        {
            this.fish = this.GetComponent<FishBody>();
            this.fish.Init();

            this.InitActivations();
            this.UpdateAnimationCurves();
            this.problem = new FishSimulatorOffline.Problem(this.fish.modelData, this.activationData);
            var dt = new IterationDelta();
            this.sim = new FishSimulatorOffline(problem, dt);
            this.sim.TryToRun();
        }

        protected void Update()
        {
            if (this.mode == ControlMode.Manual)
            {
                var types = new List<Spring.Type>() { Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
                foreach (var t in types)
                {
                    var a = this.activationData[t, Spring.Side.Left];
                    a.Tuning.useFFT = false;
                    a.DiscreteFunction.ResetValues(this.activation);
                    // a = this.activationData[t, Spring.Side.Right];
                    // a.Tuning.useFFT = false;
                    // var ra = (this.activation + 1) * 0.5f;
                    // ra = 1 - ra;
                    // ra = ra * 2 - 1;
                    // a.DiscreteFunction.ResetValues(ra);
                }
            }

            foreach(var a in this.activationData.ToActivationList())
            {
                a.Tuning.useFFT = this.fft;
            }
            if(this.loop && (this.sim.CurrentSolution as FishSimulatorOffline.Solution).IsDone)
            {
                this.sim.Restart();
            }
            if(Input.GetKeyDown(KeyCode.S))
            {
                FishActivationData.Save(this.activationData);
            }

            if (Input.GetKey(KeyCode.R))
            {
                this.fish.Init();
                this.problem.UpdateData(this.fish.modelData, this.activationData);    
                this.sim.Restart();
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                this.UpdateActivationDataFromCurves();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                this.activationData.RandomActivation();
                this.UpdateAnimationCurves();
            }
        }

        protected void OnDrawGizmos()
        {
            var sol = this.sim?.CurrentSolution as FishSimulatorOffline.Solution;
            using (new GizmosScope(Color.white, this.transform.localToWorldMatrix))
            {
                if (sol != null)
                {
                    var logPos = sol.logger.LogData.trajectory.ToYVector();
                    for (var i = 0; i < logPos.Size - 1; ++i)
                    {
                        Gizmos.DrawLine(logPos[i], logPos[i + 1]);
                    }
                }
            }
        }

    }
}

