using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;

namespace UnityFishSimulation
{
    public class FishOfflineRunner : MonoBehaviour
    {
        protected FishBody body;
        protected FishActivationData activationData;

        [SerializeField] protected float2 interval = new float2(0,20);
        [SerializeField] protected int sampleNum = 15;
        [SerializeField] protected List<AnimationCurve> curves;
        [SerializeField] protected List<ActivationData> activations;

        protected void Start()
        {
            this.body = this.GetComponent<FishBody>();
            this.body.Init();

            // this.activationData = new FishActivationDataSwimming(this.interval, this.sampleNum);
            //this.activationData.RandomActivation();
            this.activationData = FishActivationData.Load();
            this.interval = this.activationData.Interval;
            this.sampleNum = this.activationData.SampleNum;

            this.curves = this.activationData.ToAnimationCurves();
            this.activations = this.activationData.ToActivationList();
            
            var problem = new FishSimulatorOffline.Problem(this.body.modelData, this.activationData);
            var dt = new IterationDelta();
            var sim = new FishSimulatorOffline(problem, dt);
            sim.TryToRun();
        }
    }
}