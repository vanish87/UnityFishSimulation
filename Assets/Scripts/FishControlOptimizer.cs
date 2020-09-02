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
    public class FishActivationDataTrun : FishActivationData
    {
        [SerializeField] protected float speed = 1;
        protected override string FileName => "TurnRight";
        protected override List<Spring.Type> GetSprtingTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle };
        }

        public FishActivationDataTrun(float2 interval, int sampleNum = 15) : base(interval, sampleNum) { }
    }
    [Serializable]
    public class TuningData
    {
        [Serializable]
        public class SpringToData
        {
            public Spring.Type type;
            public float offset = 0;
            public float amplitude = 1;
            public float frequency = 1;
        }

        public List<SpringToData> springToDatas = new List<SpringToData>();
        public bool useFFT = true;

        public SpringToData GetDataByType(Spring.Type type)
        {
            var ret = this.springToDatas.Find(sd => sd.type == type);
            if (ret == null)
            {
                ret = new SpringToData() { type = type };
                this.springToDatas.Add(ret);
            }
            return ret;
        }
    }
    [Serializable]
    public class FFTData
    {
        [Serializable]
        public class CosData
        {
            public float amplitude = 1;
            public float frequency = 1;
            public float phase = 1;

            public float Evaluate(float x)
            {
                return this.amplitude * math.cos(this.frequency * x + this.phase);
            }
        }

        protected X2FDiscreteFunction<float> sourceFunction;
        protected List<CosData> cosDatas = new List<CosData>();
        public FFTData(X2FDiscreteFunction<float> activation)
        {
            this.sourceFunction = activation;
        }
        public float Evaluate(float x, int level = 1, bool sort = true)
        {
            var cosFunc = sort ? this.cosDatas.OrderByDescending(a => a.amplitude).ToList() : this.cosDatas;

            var count = 0;
            var ret = 0f;
            for (int i = 1; i < cosFunc.Count && count++ < level; ++i)
            {
                if (math.abs(cosFunc[i].amplitude) <= 0.01f) continue;
                ret += cosFunc[i].Evaluate(x);
            }
            return ret + 0.5f;

        }
        public void GenerateFFTData()
        {
            var vector = this.sourceFunction.ToYVector();

            var array = vector.Select(s => (double)s).ToArray();
            var dft = new DFT();

            dft.Initialize((uint)array.Length);
            Complex[] cSpectrum = dft.Execute(array);

            var An = DSP.ConvertComplex.ToMagnitude(cSpectrum);
            var Pn = DSP.ConvertComplex.ToPhaseRadians(cSpectrum);

            LogTool.AssertIsTrue(An.Length == Pn.Length);

            this.cosDatas.Clear();
            for (var i = 0; i < An.Length; ++i)
            {
                this.cosDatas.Add(new CosData() { amplitude = (float)An[i], frequency = i, phase = (float)Pn[i] });
            }
        }

        public AnimationCurve ToAnimationCurve()
        {
            var temp = this.sourceFunction.DeepCopy();
            for(var i = 0; i < temp.SampleNum; ++i)
            {
                var x = 2 * math.PI * i / (temp.SampleNum - 1);
                temp[i] = this.Evaluate(x);
            }

            return temp.ToAnimationCurve();
        }
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

        public static void UpdateFFT(List<X2FDiscreteFunction<float>> activations, int fftLevel = 1, bool ordered = false)
        {
            foreach (var func in activations)
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
        public static float GetFx(double[] An, double[] Pn, float x, int level = 1, bool ordered = false)
        {
            var orderedAn = ordered ? An.OrderByDescending(a => a).ToArray() : An;

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
        public int SampleNum { get => this.sampleNum; }
        public int FunctionCount { get => this.activations.Count; }

        protected float2 interval;
        protected int sampleNum;

        protected Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();
        protected Dictionary<Spring.Type, FFTData> fftData = new Dictionary<Spring.Type, FFTData>();
        protected TuningData tuningData = new TuningData();

        protected abstract List<Spring.Type> GetSprtingTypes();
        protected abstract string FileName { get; }


        public TuningData Tuning { get => this.tuningData = this.tuningData ?? new TuningData(); }
        //public Dictionary<Spring.Type, X2FDiscreteFunction<float>> Activations { get => this.activations; }

        public X2FDiscreteFunction<float> this[Spring.Type type]
        {
            set => this.activations[type] = value;
        }

        public FishActivationData() : this(new float2(0,1)){}
        public FishActivationData(float2 interval, int sampleNum = 15)
        {
            this.interval = interval;
            this.sampleNum = sampleNum;

            this.activations.Clear();
            this.tuningData.springToDatas.Clear();

            var types = this.GetSprtingTypes();
            var start = new Tuple<float, float>(interval.x, 0);
            var end = new Tuple<float, float>(interval.y, 0);
            foreach (var t in types)
            {
                var func = new X2FDiscreteFunction<float>(start, end, this.sampleNum);
                this.activations.Add(t, func);
                this.fftData.Add(t, new FFTData(func));
                this.tuningData.springToDatas.Add(new TuningData.SpringToData() { type = t });
            }
        }
        public bool HasType(Spring.Type type) { return this.activations.ContainsKey(type); }
        public float Evaluate(float x, Spring.Type type, bool fft = true)
        {
            var ret = 0f;
            if (this.HasType(type))
            {
                ret = fft ? (this.fftData[type].Evaluate(x)) : this.activations[type].Evaluate(x);
            }

            return ret;
        }
        public void UpdateFromVector(Vector<float> x)
        {
            this.activations = this.VectorToActivation(x, this.interval, this.sampleNum);
        }
        public void GenerateFFTData()
        {
            if(this.fftData == null)
            {
                this.fftData = new Dictionary<Spring.Type, FFTData>();
                foreach (var func in this.activations)
                {
                    this.fftData.Add(func.Key, new FFTData(func.Value));
                }
            }
            foreach(var fft in this.fftData.Values)
            {
                fft.GenerateFFTData();
            }
        }
        public List<X2FDiscreteFunction<float>> ToDiscreteFunctions()
        {
            return this.activations.Values.ToList();
        }
        public List<AnimationCurve> ToAnimationCurves() 
        {
            var ret = new List<AnimationCurve>();
            foreach(var func in this.activations)
            {
                ret.Add(func.Value.ToAnimationCurve());
                var fft = this.fftData?[func.Key];
                if (fft != null)
                { 
                    ret.Add(fft.ToAnimationCurve());
                }
            }
            return ret;
        }
    }
    public class FishControlOptimizer : MonoBehaviour
    {
        protected static float GetCurrentE(FishSimulator.Solution sol, List<X2FDiscreteFunction<float>> activations, int sampleSize)
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


            var goalPos = new float3(0, 0, -100);
            var orgPos = new float3(0, 0, 0);
            var goalVel = 10f;

            var Ev = useSol ? math.length(trajactory.End.Item2 - goalPos) / math.length(goalPos - orgPos) : 0;
            //Ev += useSol ? -trajactory.End.Item2.x / goalVel : 0;


            for (int i = 0; i < sampleSize; ++i)
            {
                var Eu = 0f;
                //var Ev = useSol ? math.length(trajactory[i] - goalPos) : 0;
                //Ev /= math.length(goalPos - orgPos) * sampleSize;
                //Ev += useSol ? -velocity[i].x : 0;
                //var Ev = 0;


                var du = 0f;
                var du2 = 0f;
                foreach (var fun in activations)
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
                            case OptType.Turn: this.activationData = new FishActivationDataTrun(para.interval, para.sampleNum); break;
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
                                simulator.ResetAndRun();

                                //Debug.Log("start");
                                //start new simulation to get trajactory
                                //wait to finish
                                while (simulator.IsSimulationDone() == false) { }
                            }

                            var e = GetCurrentE(simulator?.CurrentSolution as FishSimulator.Solution, this.activationData.ToDiscreteFunctions(), this.activationData.SampleNum);

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
                        foreach (var func in this.activationData.ToDiscreteFunctions())
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
                this.dim = this.fishActivationData.FunctionCount * this.sampleNum;
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

                var e = GetCurrentE(simulator?.CurrentSolution as FishSimulator.Solution, this.fishActivationData.ToDiscreteFunctions(), this.sampleNum);

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
            this.StartSA(new SAProblem(this.timeInterval, this.sampleNum, SAProblem.OptType.Turn));
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