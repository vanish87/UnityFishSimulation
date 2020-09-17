using DSPLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityTools;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishControlOptimizer : MonoBehaviour
    {
        protected static float GetCurrentE(FishLogger logger, FishActivationData activationData)
        {
            var mu1 = 0.5f;
            var mu2 = 0.5f;

            //As the value of v1 increases, 
            //both the amplitude and the frequency decrease
            //v1 = 0; 0.1; 0.2
            var v1 = 0.001f;
            //As the value of v2 increases, 
            //the amplitude remains constant and only the frequency decreases.
            //v2 = 0; 0.002; 0.006
            var v2 = 0.002f;

            var E = 0f;

            var goalPos = new float3(0, 0, 100);
            var orgPos = new float3(0, 0, 0);
            var goalVel = 10f;

            var Ev = 0f;
            foreach (var pos in logger.LogData.trajectory.ToYVector())
            {
                Ev += math.distance(pos, goalPos) / math.distance(orgPos, goalPos);
            }

            Ev += -logger.LogData.velocity.ToYVector().Last().x / goalVel;


            var Eu = 0f;
            {
                foreach (var activation in activationData.ToActivationList())
                {
                    var du = 0f;
                    var du2 = 0f;
                    var func = activation.DiscreteFunction;
                    for (var i = 0; i < func.SampleNum; ++i)
                    {
                        var dev = func.Derivate(i);
                        var dev2 = func.Derivate2(i);
                        du += dev * dev;
                        du2 += dev2 * dev2;
                    }
                    Eu += 0.5f * (v1 * du + v2 * du2);
                }


            }
            E = mu1 * Eu + mu2 * Ev;

            return E;
        }
        [Serializable]
        public class SAProblem : SimulatedAnnealing.Problem
        {
            public enum OptType
            {
                Swimming,
                TurnLeft,
                TurnRight,
            }
            [Serializable]
            public class Parameter
            {
                public OptType type;
                public float2 interval;
                public int sampleNum;
            }
            [Serializable]
            public class ActivationState : CircleData<ActivationState.Data, Parameter>
            {
                [Serializable]
                public class Data : SimulatedAnnealing.IState
                {
                    public float LatestE { get; private set; }
                    public FishActivationData ActivationData { get => this.activationData; }
                    protected FishActivationData activationData;

                    protected bool isDirty = true;

                    public Data() { Assert.IsTrue(false); }
                    public Data(Parameter para)
                    {
                        switch (para.type)
                        {
                            case OptType.Swimming: this.activationData = new FishActivationDataSwimming(para.interval, para.sampleNum); break;
                            case OptType.TurnLeft: this.activationData = new FishActivationDataTurnLeft(para.interval, para.sampleNum); break;
                            case OptType.TurnRight: this.activationData = new FishActivationDataTurnRight(para.interval, para.sampleNum); break;
                        }

                        this.Generate(this);
                    }

                    public float Evaluate(SimulatedAnnealing.IState state)
                    {
                        if (this.isDirty)
                        {
                            var logger = new FishLogger();
                            var useSim = true;
                            if (useSim)
                            {
                                //start new simulation to get trajactory
                                var body = GeometryFunctions.Load();
                                var problem = new FishSimulatorOffline.Problem(body, this.activationData);
                                var dt = new IterationDelta();
                                var sim = new FishSimulatorOffline(problem, dt);
                                sim.TryToRun();
                                // wait to finish
                                var sol = sim.CurrentSolution as FishSimulatorOffline.Solution;
                                while (sol.IsDone == false) { }
                                logger = sol.logger.DeepCopy();
                                sim.Dispose();
                            }

                            var e = GetCurrentE(logger, this.activationData);

                            this.LatestE = e;
                            this.isDirty = false;

                            // Debug.Log("end with e = " + this.LatestE);
                        }
                        //cal new E from RandomX2FDiscreteFunction and trajactory

                        return this.LatestE;
                    }
                    public SimulatedAnnealing.IState Generate(SimulatedAnnealing.IState x)
                    {
                        this.isDirty = true;
                        this.activationData.RandomActivation();
                        // foreach (var func in this.activationData.ToDiscreteFunctions())
                        {
                            // func.RandomValues();
                            /*for (var i = 0; i < func.SampleNum; ++i)
                            {
                                //rand in [-1, 1)
                                var rand = (ThreadSafeRandom.NextFloat() - 0.5f) * 2;
                                //6% of random bound
                                func[i] = Mathf.Clamp01(func[i] + rand * 0.5f);
                            }*/
                        }
                        return x;
                    }
                }

                public ActivationState(Parameter para, int size = 2) : base(size, para)
                {
                }

                protected override Data OnCreate(Parameter para)
                {
                    return new Data(para);
                }
            }

            protected ActivationState state;
            public override SimulatedAnnealing.IState Current => this.state.Current;

            public override SimulatedAnnealing.IState Next => this.state.Next;

            public SAProblem() { }
            public SAProblem(float2 interval, int sampleNum, OptType type = OptType.Swimming) : base()
            {
                this.state = new ActivationState(new Parameter() { interval = interval, sampleNum = sampleNum, type = type });
            }

            public override void MoveToNext()
            {
                this.state.MoveToNext();
            }

            [SerializeField] protected int maxCount = 0;
            [SerializeField] protected int minCount = 0;
            public override void Cool(bool useNext)
            {
                if (useNext) this.minCount++;
                else this.maxCount++;

                var data = this.Current as ActivationState.Data;
                var min = data.ActivationData.SampleNum * data.ActivationData.FunctionCount;
                var max = 10 * min;
                var shouldCool = min < this.minCount || max < this.maxCount;


                //LogTool.Log("Cooling Count min: " + this.minCount + " max: " + this.maxCount);
                if (shouldCool)
                {
                    LogTool.Log("Temperature is " + this.temperature, LogLevel.Info);
                    LogTool.Log("Current is " + (this.Current as ActivationState.Data).LatestE);
                    LogTool.Log("Next is " + (this.Next as ActivationState.Data).LatestE);

                    this.maxCount = this.minCount = 0;
                }

                base.Cool(shouldCool);
            }
        }

        public class Delta : IDelta
        {
            internal protected int count = 0;
            public void Reset()
            {
                count = 0;
            }

            public void Step()
            {
                count++;
            }
        }

        [SerializeField] protected float2 timeInterval = new float2(0, 20);
        [SerializeField] protected int sampleNum = 15;
        [SerializeField] protected string fileName = "SAProblem.data";

        protected IterationAlgorithm algorithm;
        protected IProblem p;
        protected ISolution sol;


        protected FishSimulator simulator;

        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
        [SerializeField] protected SAProblem sa;

        protected void StartSA(SAProblem sa)
        {
            p = sa;
            this.sa = sa;
            var d = new Delta();
            this.algorithm = new SimulatedAnnealing(p, d);
            this.algorithm.TryToRun();

            /*this.algorithm.PerStep((p, s, dt, a) =>
            {
                LogTool.Log("Solution Updated: ", LogLevel.Info);
                var pro = p as SAProblem;

                LogTool.Log("Count is " + (dt as Delta).count);
                LogTool.Log("Temp is " + pro.temperature);
                LogTool.Log("Current is " + (pro.Current as SAProblem.ActivationState.Data).LatestE);
                LogTool.Log("Next is " + (pro.Next as SAProblem.ActivationState.Data).LatestE);
            });*/

        }

        protected FishActivationData GetActivationData()
        {
            if (this.algorithm.CurrentSolution is DownhillSimplex<float>.Solution)
            {
                var sol = (this.algorithm.CurrentSolution) as DownhillSimplex<float>.Solution;
                var ret = new FishActivationDataSwimming(this.timeInterval, this.sampleNum);
                // ret.UpdateFromVector(sol.min.X);
                LogTool.AssertIsTrue(false);
                return ret;
            }
            else
            {
                var problem = (this.p) as SAProblem;
                return (problem.Current as SAProblem.ActivationState.Data).ActivationData;
            }
        }

        public void SaveData(SAProblem problem, string fileName = "SAProblem.data")
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            FileTool.Write(path, problem);
            LogTool.Log("Saved " + path);
        }

        public SAProblem LoadData(string fileName = "SAProblem.data")
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            var ret = FileTool.Read<SAProblem>(path);
            LogTool.Log("Loaded " + path);

            return ret;
        }


        protected void Start()
        {
            var p = new SAProblem(this.timeInterval, this.sampleNum, SAProblem.OptType.Swimming) 
            {
                temperature = 5000, 
                minTemperature = 1 
            };
            this.StartSA(p);
        }

        FishActivationData current;
        protected void Update()
        {
            /*if (Input.GetKeyDown(KeyCode.R))
            {
                var problem = new FishSimulator.Problem(this.current);
                var delta = new FishSimulator.Delta();

                if (this.simulator != null) this.simulator.Dispose();
                this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                this.simulator.ResetAndRun();
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                var activations = this.GetActivationData().DeepCopy();

                var compare = activations.DeepCopy();

                FishActivationData.UpdateFFT(activations.ToDiscreteFunctions(), this.fftLevel);

                this.current = activations;

                var problem = new FishSimulator.Problem(activations);
                var delta = new FishSimulator.Delta();

                if(this.simulator != null)this.simulator.Dispose();
                this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                this.simulator.ResetAndRun();

                this.curves.Clear();
                this.curves.AddRange(activations.ToAnimationCurves());
                this.curves.AddRange(compare.ToAnimationCurves());
            }*/

            if (Input.GetKeyDown(KeyCode.S))
            {
                var problem = (this.p) as SAProblem;
                if (problem != null)
                {
                    this.SaveData(problem, this.fileName);
                }
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                var problem = (this.p) as SAProblem;
                var act = (problem.Current as SAProblem.ActivationState.Data).ActivationData;
                FishActivationData.Save(act);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                var problem = (this.p) as SAProblem;
                if (problem != null)
                {
                    var data = this.LoadData(this.fileName);
                    if (data != null)
                    {
                        this.algorithm.Dispose();

                        this.p = data;
                        this.algorithm = new SimulatedAnnealing(this.p, new Delta());

                        this.algorithm.Start((p, s, dt, a) =>
                        {
                            LogTool.Log("Start Running");

                            var pro = p as SAProblem;
                            LogTool.Log("Temperature is " + pro.temperature);
                            LogTool.Log("Current is " + (pro.Current as SAProblem.ActivationState.Data).LatestE);
                            LogTool.Log("Next is " + (pro.Next as SAProblem.ActivationState.Data).LatestE);
                        });

                        this.algorithm.PerStep((p, s, dt, a) =>
                        {
                            var c = (dt as Delta).count;
                            if (c % 50 == 0)
                            {
                                LogTool.Log("Step Count is " + c);
                            }
                        });


                        this.algorithm.TryToRun();
                    }
                }
            }
        }

        protected void OnDrawGizmos()
        {/*
            this.simulator?.OnGizmos();*/
        }


    }
}