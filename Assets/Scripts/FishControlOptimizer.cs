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
    [Serializable]
    public class FishActivationDataSwimming: FishActivationData
    {
        [SerializeField] protected float speed = 1;
        protected override string FileName => "Swimming";
        protected override List<Spring.Type> GetSprtingTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
        }

        public FishActivationDataSwimming(float2 interval, int sampleNum = 15) : base(interval, sampleNum) { }
    }
    [Serializable]
    public abstract class FishActivationData
    {
        protected Dictionary<Spring.Type, X2FDiscreteFunction<float>> VectorToActivation(Vector<float> x, float2 interval, int sampleNum)
        {
            var activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();

            var types = this.GetSprtingTypes();
            var count = 0;
            foreach(var t in types)
            {
                activations.Add(t, new X2FDiscreteFunction<float>(interval.x, interval.y, Vector<float>.Sub(sampleNum* count, sampleNum * count, x)));
                count++;
            }
            return activations;
        }

        public static void UpdateFFT(Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations, int fftLevel = 1, bool ordered = false)
        {
            foreach (var func in activations.Values)
            {
                var vector = func.ToYVector();

                var array = vector.Select(s => (double)s).ToArray();
                var dft = new DFT();

                dft.Initialize((uint)array.Length);
                Complex[] cSpectrum = dft.Execute(array);

                var An = DSP.ConvertComplex.ToMagnitude(cSpectrum);
                var Pn = DSP.ConvertComplex.ToPhaseRadians(cSpectrum);

                for (var i = 0; i < func.SampleNum; ++i)
                {
                    var x = 2 * math.PI * i / (func.SampleNum - 1);
                    func[i] = GetFx(An, Pn, x, fftLevel, ordered);
                }
            }
        }
        protected static float GetFx(double[] An, double[] Pn, float x, int level = 1, bool ordered = false)
        {
            var orderedAn = ordered ? An : An.OrderByDescending(a => a).ToArray();

            var count = 0;
            var ret = 0f;
            for (int i = 0; i < An.Length && count++ < level + 1; ++i)
            {
                var an = orderedAn[i];
                var id = An.ToList().IndexOf(an);
                var pn = Pn[id];

                if (math.abs(an) <= 0.01f) continue;
                ret += (float)(an * math.cos(id * x + pn));
            }
            return ret;
        }
        public int SampleNum { get => this.sampleNum; }

        protected float2 interval;
        protected int sampleNum;

        protected Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations;

        protected abstract List<Spring.Type> GetSprtingTypes();
        protected abstract string FileName { get; }

        public static void Save(FishActivationData data)
        {
            var fileName = data.FileName + ".ad";
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            FileTool.Write(path, data);
            LogTool.Log("Saved " + path);
        }

        public static FishActivationData Load(string fileName = "Swimming")
        {
            fileName += ".ad";
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            var ret = FileTool.Read<FishActivationData>(path);
            LogTool.Log("Loaded " + path);
            return ret;
        }

        public Dictionary<Spring.Type, X2FDiscreteFunction<float>> Activations { get => this.activations; }

        public FishActivationData() : this(new float2(0,1)){}
        public FishActivationData(float2 interval, int sampleNum = 15)
        {
            this.interval = interval;
            this.sampleNum = sampleNum;

            this.activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();

            var types = this.GetSprtingTypes();
            var start = new Tuple<float, float>(interval.x, 0);
            var end = new Tuple<float, float>(interval.y, 0);
            foreach (var t in types)
            { 
                this.activations.Add(t, new X2FDiscreteFunction<float>(start, end, this.sampleNum));
            }
        }

        public void UpdateFromVector(Vector<float> x)
        {
            this.activations = this.VectorToActivation(x, this.interval, this.sampleNum);
        }
    }
    public class FishControlOptimizer : MonoBehaviour
    {
        protected static float GetCurrentE(FishSimulator.Solution sol, Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations, int sampleSize)
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

            var useSol = sol != null;

            var trajactory = sol?.trajactory;
            var velocity = sol?.velocity;


            var goalPos = new float3(100, 0, 0);
            var orgPos = new float3(0, 0, 0);
            var goalVel = 10f;

            var Ev = useSol ? math.length(trajactory.End.Item2 - goalPos) / math.length(goalPos - orgPos) : 0;
            Ev += useSol ? -trajactory.End.Item2.x / goalVel : 0;


            for (int i = 0; i < sampleSize; ++i)
            {
                var Eu = 0f;
                //var Ev = useSol ? math.length(trajactory[i] - goalPos) : 0;
                //Ev /= math.length(goalPos - orgPos) * sampleSize;
                //Ev += useSol ? -velocity[i].x : 0;
                //var Ev = 0;


                var du = 0f;
                var du2 = 0f;
                foreach (var fun in activations.Values)
                {
                    var dev = fun.Devrivate(i);
                    var dev2 = fun.Devrivate2(i);
                    du += dev * dev;
                    du2 += dev2 * dev2;
                }

                Eu = 0.5f * (v1 * du + v2 * du2);

                E += mu1 * Eu + mu2 * Ev;
            }

            return E;
        }
        [Serializable]
        public class SAProblem : SimulatedAnnealing.Problem
        {
            public enum OptType
            {
                Swimming,
                Turn,
            }
            [Serializable]
            public class Paramter
            {
                public OptType type;
                public float2 interval;
                public int sampleNum;
            }
            [Serializable]
            public class ActivationState : CricleData<ActivationState.Data, Paramter>
            {
                [Serializable]
                public class Data : SimulatedAnnealing.IState
                {
                    public float LatestE { get; private set; }
                    public FishActivationData ActivationData { get => this.activationData; }
                    protected FishActivationData activationData;

                    protected bool isDirty = true;

                    public Data() { Assert.IsTrue(false); }
                    public Data(Paramter para)
                    {
                        switch(para.type)
                        {
                            case OptType.Swimming: this.activationData = new FishActivationDataSwimming(para.interval, para.sampleNum);break;
                            case OptType.Turn: this.activationData = new FishActivationDataSwimming(para.interval, para.sampleNum); break;
                        }
                        
                        this.Generate(this);
                    }

                    public float Evaluate(SimulatedAnnealing.IState state)
                    {
                        if (this.isDirty)
                        {
                            var useSim = true;
                            FishSimulator simulator = null;
                            if (useSim)
                            {
                                var problem = new FishSimulator.Problem(this.activationData);
                                var delta = new FishSimulator.Delta();


                                simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                                simulator.TryToRun();

                                //Debug.Log("start");
                                //start new simulation to get trajactory
                                //wait to finish
                                while (simulator.IsSimulationDone() == false) { }
                            }

                            var e = GetCurrentE(simulator?.CurrentSolution as FishSimulator.Solution, this.activationData.Activations, this.activationData.SampleNum);

                            if (useSim) simulator.StopThread();

                            this.LatestE = e;
                            this.isDirty = false;
                        }
                        //Debug.Log("end with e = " + e);
                        //cal new E from RandomX2FDiscreteFunction and trajactory

                        return this.LatestE;
                    }
                    public SimulatedAnnealing.IState Generate(SimulatedAnnealing.IState x)
                    {
                        this.isDirty = true;
                        foreach (var func in this.activationData.Activations.Values)
                        {
                            func.RandomValues();
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

                public ActivationState(Paramter para, int size = 2) : base(size, para)
                {
                }

                protected override Data OnCreate(Paramter para)
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
                this.state = new ActivationState(new Paramter() { interval = interval, sampleNum = sampleNum, type = type });
            }

            public override void MoveToNext()
            {
                this.state.MoveToNext();
            }

            int maxCount = 0;
            int minCount = 0;
            public override void Cool(bool useNext)
            {
                if (useNext) this.minCount++;
                else this.maxCount++;

                var data = this.Current as ActivationState.Data;
                var min = data.ActivationData.SampleNum * data.ActivationData.Activations.Count;
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
        public class Problem : DownhillSimplex<float>.Problem
        {
            protected FishActivationData fishActivationData;
            protected float2 interval;
            protected int sampleNum;
            public Problem(float2 interval, int sampleNum) : base(0)
            {
                this.interval = interval;
                this.sampleNum = sampleNum;

                this.fishActivationData = new FishActivationDataSwimming(this.interval, this.sampleNum);
                this.dim = this.fishActivationData.Activations.Count * this.sampleNum;
            }

            public override float Evaluate(Vector<float> x)
            {
                //from vector x
                //convert to X2FDiscreteFunction
                this.fishActivationData.UpdateFromVector(x);

                var useSim = true;
                FishSimulator simulator = null;
                if (useSim)
                {
                    var problem = new FishSimulator.Problem(this.fishActivationData);
                    var delta = new FishSimulator.Delta();


                    simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                    simulator.TryToRun();

                    //Debug.Log("start");
                    //start new simulation to get trajactory
                    //wait to finish
                    while (simulator.IsSimulationDone() == false) { }
                }

                var e = GetCurrentE(simulator?.CurrentSolution as FishSimulator.Solution, this.fishActivationData.Activations, this.sampleNum);

                if(useSim) simulator.StopThread();

                Debug.Log("end with e = " + e);
                //cal new E from RandomX2FDiscreteFunction and trajactory

                return e;
            }

            public override Vector<float> Generate(Vector<float> x)
            {
                if (x == null) x = new Vector<float>(this.dim);

                for (var i = 0; i < x.Size; ++i)
                {
                    x[i] = ThreadSafeRandom.NextFloat();
                }

                return x;
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
        [SerializeField] protected int fftLevel = 1;
        [SerializeField] protected string fileName = "SAProblem.data";

        protected IterationAlgorithm algprithm;
        protected IProblem p;
        protected ISolution sol;


        protected FishSimulator simulator;

        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();

        protected void StartDS()
        {
            p = new Problem(this.timeInterval, this.sampleNum);
            var d = new Delta();
            this.algprithm = new DownhillSimplex<float>(p, d);
            this.algprithm.TryToRun();

            this.algprithm.PerStep((p, s, dt, a) =>
            {
                LogTool.Log("Solution Updated: ", LogLevel.Info);
                var sol = s as DownhillSimplex<float>.Solution;
                sol.min.Print();
            });

        }
        protected void StartSA(SAProblem sa)
        {
            p = sa;
            var d = new Delta();
            this.algprithm = new SimulatedAnnealing(p, d);
            this.algprithm.TryToRun();

            /*this.algprithm.PerStep((p, s, dt, a) =>
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
            if (this.algprithm.CurrentSolution is DownhillSimplex<float>.Solution)
            {
                var sol = (this.algprithm.CurrentSolution) as DownhillSimplex<float>.Solution;
                var ret = new FishActivationDataSwimming(this.timeInterval, this.sampleNum);
                ret.UpdateFromVector(sol.min.X);
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
            this.StartSA(new SAProblem(this.timeInterval, this.sampleNum));
        }

        FishActivationData current;
        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                var problem = new FishSimulator.Problem(this.current);
                var delta = new FishSimulator.Delta();

                if (this.simulator != null) this.simulator.Dispose();
                this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                this.simulator.TryToRun();
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                var activations = this.GetActivationData().DeepCopy();

                var compare = activations.DeepCopy();

                FishActivationData.UpdateFFT(activations.Activations, this.fftLevel);

                this.current = activations;

                var problem = new FishSimulator.Problem(activations);
                var delta = new FishSimulator.Delta();

                if(this.simulator != null)this.simulator.Dispose();
                this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                this.simulator.TryToRun();

                this.curves.Clear();
                foreach(var act in activations.Activations.Values)
                {
                    this.curves.Add(act.ToAnimationCurve());
                }


                foreach (var act in compare.Activations.Values)
                {
                    this.curves.Add(act.ToAnimationCurve());
                }
            }

            if(Input.GetKeyDown(KeyCode.S))
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
            if(Input.GetKeyDown(KeyCode.L))
            {
                var problem = (this.p) as SAProblem;
                if (problem != null)
                {
                    var data = this.LoadData(this.fileName);
                    if(data != null)
                    {
                        this.algprithm.Dispose();

                        this.p = data;
                        this.algprithm = new SimulatedAnnealing(this.p, new Delta());

                        this.algprithm.Start((p, s, dt, a) =>
                        {
                            LogTool.Log("Start Running");

                            var pro = p as SAProblem;
                            LogTool.Log("Temperature is " + pro.temperature);
                            LogTool.Log("Current is " + (pro.Current as SAProblem.ActivationState.Data).LatestE);
                            LogTool.Log("Next is " + (pro.Next as SAProblem.ActivationState.Data).LatestE);
                        });

                        this.algprithm.PerStep((p, s, dt, a) =>
                        {
                            var c = (dt as Delta).count;
                            if (c % 50 == 0)
                            {
                                LogTool.Log("Step Count is " + c);
                            }
                        });


                        this.algprithm.TryToRun();
                    }
                }
            }
        }

        protected void OnDrawGizmos()
        {
            this.simulator?.OnGizmos();
        }


    }
}