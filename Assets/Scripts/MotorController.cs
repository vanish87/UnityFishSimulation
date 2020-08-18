using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Math;
using static UnityFishSimulation.FishSAOptimizer.FishSA;

namespace UnityFishSimulation
{        

    public class FunctionDrawer
    {
        public void OnGizmos(float3 offset)
        {
            /*if (this.valueMap != null)
            {
                using (new GizmosScope(Color.cyan, Matrix4x4.TRS(new Vector3(offset.x, offset.y, offset.z), Quaternion.identity, Vector3.one)))
                {
                    for (var i = 1; i < this.valueMap.Current.Count; ++i)
                    {
                        var from = new Vector3(this.valueMap.Current[i - 1].Item1, this.valueMap.Current[i - 1].Item2);
                        var to = new Vector3(this.valueMap.Current[i].Item1, this.valueMap.Current[i].Item2);
                        Gizmos.DrawLine(from, to);
                    }
                }
            }*/
        }
    }

    public class MotorController : MonoBehaviour
    {
        public const float Smax = 0.075f;

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

        public enum SAState
        {
            Start,
            FishTrajectorySimulating,
            SAEvaluating,
            NextFishTrajectorySimulating,
            SADone,
        }

        public enum ControllerMode
        {
            Learning,
            LearningWithSimulation,
            Preview,
        }

        

        [SerializeField] protected FishSimulator fishSimulator;

        [SerializeField] protected Dictionary<Spring.Type, RandomX2FDiscreteFunction> 
            activations = new Dictionary<Spring.Type, RandomX2FDiscreteFunction>();

        [SerializeField] protected float2 timeInterval = new float2(0, 5);
        [SerializeField] protected int sampleSize = 15;
        [SerializeField] protected float h;

        [SerializeField] protected float temperature = 10000;
        [SerializeField] protected float minTemperature = 0.0001f;
        [SerializeField] protected float alpha = 0.99f;
        [SerializeField] protected float successCount = 0;

        [SerializeField] protected float currentE = 0;


        [SerializeField, Range(0, 1)] protected float act = 0.5f;

        [SerializeField] protected SAState currentState = SAState.Start;
        [SerializeField] protected bool startNewLearning = false;
        [SerializeField] protected int runningCount = 0;
        [SerializeField] protected string fileName = "s30";
        [SerializeField] protected ControllerMode mode = ControllerMode.Learning;

        protected void Start()
        {
            this.activations.Clear();

            this.h = (this.timeInterval.y - this.timeInterval.x) / sampleSize;

            var start = new Tuple<float, float>(this.timeInterval.x, 0.5f);
            var end   = new Tuple<float, float>(this.timeInterval.y, 0.5f);

            //this.activations.Add(Spring.Type.MuscleFront, new X2FDiscreteFunction<float>(start, end, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleMiddle, new RandomX2FDiscreteFunction(start, end, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleBack, new RandomX2FDiscreteFunction(start, end, this.sampleSize));


            if (this.startNewLearning == false) this.Load(this.fileName);

            var p = new FishSimulator.Problem(this.activations);
            var delta = new FishSimulator.Delta();
            this.fishSimulator = new FishSimulator(FishSimulator.SolverType.Euler, p, delta);
            this.fishSimulator.StartSimulation();
            //this.fishSimulator.SetStartEnd(start.Item1, end.Item1);
            //this.fishSimulator.SetActivations(this.activations);

        }

        protected void Load(string file = "Swimming")
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, file+ ".func");
            if (File.Exists(path) == false) return;

            this.activations = FileTool.Read<Dictionary<Spring.Type, RandomX2FDiscreteFunction>>(path);
            LogTool.Log("Loaded " + path);
        }

        protected void Save(string file = "Swimming")
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, file + ".func");
            FileTool.Write(path, this.activations);
            LogTool.Log("Saved " + path);
        }

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.A)) this.Save(this.fileName);


            foreach(var a in this.activations.Values)
            {
                foreach(var i in System.Linq.Enumerable.Range(0,this.sampleSize))
                {
                    a[i] = this.act;
                }
            }
            if (this.fishSimulator.IsSimulationDone()) this.fishSimulator.StartSimulation();

            /*switch(this.currentState)
            {
                case SAState.Start:
                    {
                        this.currentState = SAState.FishTrajectorySimulating;
                        if (this.mode == ControllerMode.LearningWithSimulation ||
                            this.mode == ControllerMode.Preview)
                        {
                            this.fishSimulator.StartSimulation();
                        }
                    }
                    break;
                case SAState.FishTrajectorySimulating:
                    {
                        if (this.mode == ControllerMode.Learning) this.currentState = SAState.SAEvaluating;
                        else
                        if (this.fishSimulator.IsSimulationDone())
                        {
                            this.currentState = this.mode == ControllerMode.Preview?SAState.Start:SAState.SAEvaluating;

                            this.currentE = this.GetCurrentE();
                        }
                    }
                    break;
                case SAState.SAEvaluating:
                    {
                        if (this.temperature > this.minTemperature)
                        {
                            foreach (var a in this.activations.Values)
                            {
                                a.RandomNextValues();
                                a.MoveToNext();
                            }

                            this.currentState = SAState.NextFishTrajectorySimulating;

                            if (this.mode == ControllerMode.LearningWithSimulation)
                            {
                                this.fishSimulator.StartSimulation();
                            }
                        }
                        else
                        {
                            this.currentState = SAState.SADone;
                        }
                    }
                    break;
                case SAState.NextFishTrajectorySimulating:
                    {
                        var shouldContinue = this.mode == ControllerMode.LearningWithSimulation ? this.fishSimulator.IsSimulationDone() : true;
                        if (shouldContinue)
                        {
                            var next = this.GetCurrentE();
                            //Debug.Log("Current " + this.currentE + " Next " + next);

                            if (this.ShouldUseNext(this.currentE, next))
                            {
                                this.successCount++;
                                if (this.successCount % 10 == 0)
                                {
                                    //this.temperature = this.temperature/(1+this.alpha*this.temperature);
                                    this.temperature *= this.alpha;
                                }
                                this.Save(this.fileName);

                                this.currentE = next;
                            }
                            else
                            {
                                foreach (var a in this.activations.Values)
                                {
                                    a.MoveToPrev();
                                }
                            }

                            this.runningCount++;

                            this.currentState = SAState.SAEvaluating;
                        }
                    }
                    break;
                default:break;
            }*/
        }

        protected bool ShouldUseNext(float current, float next)
        {
            if (next < current)
            {
                return true;
            }
            else
            {
                return false;
                var p = math.pow(math.E, -(next - current) / this.temperature);
                LogTool.Log("p is " + p);

                if (p > UnityEngine.Random.value)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        protected float GetCurrentE()
        {
            var mu1 = 0.8f;
            var mu2 = 0.2f;

            var v1 = 0.01f;
            var v2 = 1f;

            var E = 0f;

            /*var trajactory = this.fishSimulator.GetOutput()?.trajactory;
            var velocity = this.fishSimulator.GetOutput()?.velocity;*/

            for (int i = 0; i < this.sampleSize; ++i)
            {
                var Eu = 0f;
                //var Ev = this.mode == ControllerMode.LearningWithSimulation?
                //    math.length(trajactory.Evaluate(i) - new float3(100,0,0)) - velocity.Evaluate(i).x
                //    :0;
                var Ev = 0;


                var du = 0f;
                var du2 = 0f;
                foreach (var fun in this.activations.Values)
                {
                    var dev = fun.Devrivate(i, this.h);
                    var dev2 = fun.Devrivate2(i, this.h);
                    du += dev * dev;
                    du2 += dev2 * dev2;
                }

                Eu = 0.5f * (v1 * du + v2 * du2);

                E += mu1 * Eu + mu2 * Ev;
            }

            return E;
        }

        public float drawtestX = 0;
        public float drawtestY = 0;
        protected void OnDrawGizmos()
        {
            if (this.activations.Count > 0)
            {
                var offset = new float3(10, 0, 0);
                foreach (var activation in this.activations.Values)
                {
                    //activation.OnGizmos(offset);
                    offset.y += 3;
                }

                //this.trajectory.OnGizmos(offset);


                /*var act = this.activations[Spring.Type.MuscleFront];
                drawtestY = act.Evaluate(drawtestX);
                Gizmos.DrawSphere(new Vector3(drawtestX + 10, drawtestY), 0.1f);*/
            }

            //this.fishSimulator?.Fish.OnGizmos(GeometryFunctions.springColorMap);
            this.fishSimulator?.OnGizmos();
        }

    }

    public class SwimMotorController : MotorController
    {
        [SerializeField] protected float speed;


    }
}
