using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DSPLib;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Math;

namespace UnityFishSimulation
{
    [Serializable]
    public class FishActivationDataSwimming : FishActivationData
    {
        protected override string FileName => "Swimming";

        public FishActivationDataSwimming(float2 interval, int sampleNum = 15) : base(interval, sampleNum) { }

        protected override List<(Spring.Type, Spring.Side)> GetSpringTypes()
        {
            return new List<(Spring.Type, Spring.Side)>()
            {
                (Spring.Type.MuscleMiddle, Spring.Side.Left),
                (Spring.Type.MuscleMiddle, Spring.Side.Right),
                (Spring.Type.MuscleBack, Spring.Side.Left),
                (Spring.Type.MuscleBack, Spring.Side.Right),
            };
        }
    }

    [Serializable]
    public class FishActivationDataTurnLeft : FishActivationData
    {
        protected override string FileName => "TurnLeft";

        public FishActivationDataTurnLeft(float2 interval, int sampleNum = 15) : base(interval, sampleNum) { }

        protected override List<(Spring.Type, Spring.Side)> GetSpringTypes()
        {
            return new List<(Spring.Type, Spring.Side)>()
            {
                (Spring.Type.MuscleFront, Spring.Side.Left),
                (Spring.Type.MuscleFront, Spring.Side.Right),
                (Spring.Type.MuscleMiddle, Spring.Side.Left),
                (Spring.Type.MuscleMiddle, Spring.Side.Right),
            };
        }
    }
    [Serializable]
    public class FishActivationDataTurnRight : FishActivationData
    {
        protected override string FileName => "TurnRight";

        public FishActivationDataTurnRight(float2 interval, int sampleNum = 15) : base(interval, sampleNum) { }

        protected override List<(Spring.Type, Spring.Side)> GetSpringTypes()
        {
            return new List<(Spring.Type, Spring.Side)>()
            {
                (Spring.Type.MuscleFront, Spring.Side.Left),
                (Spring.Type.MuscleFront, Spring.Side.Right),
                (Spring.Type.MuscleMiddle, Spring.Side.Left),
                (Spring.Type.MuscleMiddle, Spring.Side.Right),
            };
        }
    }
    [Serializable]
    public class TuningData
    {
        public float offset = 0;
        public float amplitude = 1;
        public float frequency = 1;

        public bool useFFT = true;

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
        protected List<CosData> cosData = new List<CosData>();
        public FFTData(X2FDiscreteFunction<float> activation)
        {
            this.sourceFunction = activation;
        }
        public float Evaluate(float x, int level = 1, bool sort = true)
        {
            var cosFunc = sort ? this.cosData.OrderByDescending(a => a.amplitude).ToList() : this.cosData;

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

            this.cosData.Clear();
            for (var i = 0; i < An.Length; ++i)
            {
                this.cosData.Add(new CosData() { amplitude = (float)An[i], frequency = i, phase = (float)Pn[i] });
            }
        }

        public AnimationCurve ToAnimationCurve()
        {
            var temp = this.sourceFunction.DeepCopy();
            for (var i = 0; i < temp.SampleNum; ++i)
            {
                var x = 2 * math.PI * i / (temp.SampleNum - 1);
                temp[i] = this.Evaluate(x);
            }

            return temp.ToAnimationCurve();
        }
    }
    [Serializable]
    public class ActivationData
    {
        public X2FDiscreteFunction<float> DiscreteFunction { get => this.discreteFunction; set => this.discreteFunction = value; }
        public FFTData FFT => this.fftData;
        public TuningData Tuning => this.tuningData;
        protected X2FDiscreteFunction<float> discreteFunction;
        protected FFTData fftData;
        protected TuningData tuningData;

        public ActivationData(float2 interval, int sampleNum)
        {
            var start = new Tuple<float, float>(interval.x, 0);
            var end = new Tuple<float, float>(interval.y, 0);

            this.discreteFunction = new X2FDiscreteFunction<float>(start, end, sampleNum);
            this.fftData = new FFTData(this.discreteFunction);
            this.tuningData = new TuningData();

            this.fftData.GenerateFFTData();
        }
        public float Evaluate(float x)
        {
            var value = x * this.tuningData.frequency;
            var ret = this.tuningData.useFFT ? this.fftData.Evaluate(value) : this.discreteFunction.Evaluate(value);
            return this.ApplyTuning(ret);
        }

        public List<AnimationCurve> ToAnimationCurves()
        {
            return new List<AnimationCurve>() { this.discreteFunction.ToAnimationCurve(), this.fftData.ToAnimationCurve() };
        }

        public void RandomData()
        {
            this.discreteFunction.RandomValues();
        }

        protected float ApplyTuning(float value)
        {
            var data = this.tuningData;
            value = value * data.amplitude;
            value += data.offset;
            value = math.clamp(value, -1, 1);
            return value;
        }
    }
    [Serializable]
    public abstract class FishActivationData
    {
        // protected Dictionary<Spring.Type, X2FDiscreteFunction<float>> VectorToActivation(Vector<float> x, float2 interval, int sampleNum)
        // {
        //     var activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();

        //     var types = this.GetSpringTypes();
        //     var count = 0;
        //     foreach (var t in types)
        //     {
        //         activations.Add(t, new X2FDiscreteFunction<float>(interval.x, interval.y, Vector<float>.Sub(sampleNum * count, sampleNum * count, x)));
        //         count++;
        //     }
        //     return activations;
        // }

        public static void Save(FishActivationData data)
        {
            var fileName = data.FileName + ".ad";
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            FileTool.Write(path, data);
            LogTool.Log("Saved " + path);
        }

        static Dictionary<string, FishActivationData> data = new Dictionary<string, FishActivationData>();
        public static FishActivationData Load(string fileName = "Swimming")
        {
            fileName += ".ad";
            if (data.ContainsKey(fileName)) return data[fileName];
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            var ret = FileTool.Read<FishActivationData>(path);
            LogTool.Log("Loaded " + path);
            data.Add(fileName, ret);
            return ret;
        }
        public float2 Interval => this.interval;
        public int SampleNum => this.sampleNum;
        public int FunctionCount => this.activations.Count;

        protected float2 interval;
        protected int sampleNum;

        protected Dictionary<(Spring.Type, Spring.Side), ActivationData> activations = new Dictionary<(Spring.Type, Spring.Side), ActivationData>();

        protected abstract List<(Spring.Type, Spring.Side)> GetSpringTypes();
        protected abstract string FileName { get; }

        //public Dictionary<Spring.Type, X2FDiscreteFunction<float>> Activations { get => this.activations; }

        public X2FDiscreteFunction<float> this[Spring.Type type, Spring.Side side]
        {
            set => this.activations[(type, side)].DiscreteFunction = value;
        }

        public FishActivationData() : this(new float2(0, 1)) { }
        public FishActivationData(float2 interval, int sampleNum = 15)
        {
            this.interval = interval;
            this.sampleNum = sampleNum;

            this.activations.Clear();

            var types = this.GetSpringTypes();
            foreach (var t in types)
            {
                this.activations.Add(t, new ActivationData(this.interval, this.sampleNum));
            }
        }
        public bool HasType((Spring.Type, Spring.Side) type) { return this.activations.ContainsKey(type); }
        public float Evaluate(float x, (Spring.Type, Spring.Side) type)
        {
            var ret = 0f;
            if (this.HasType(type))
            {
                ret = this.activations[type].Evaluate(x);
            }

            return ret;
        }

        public void RandomActivation()
        {
            foreach(var a in this.activations)
            {
                a.Value.RandomData();
            }
        }

        public List<AnimationCurve> ToAnimationCurves()
        {
            var ret = new List<AnimationCurve>();
            foreach (var func in this.activations.Values)
            {
                ret.AddRange(func.ToAnimationCurves());
            }
            return ret;
        }
    }
}