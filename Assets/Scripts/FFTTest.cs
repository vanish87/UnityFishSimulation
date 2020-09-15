using DSPLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Math;
using static UnityFishSimulation.FishControlOptimizer;

namespace UnityFishSimulation
{
    public class FFTTest : MonoBehaviour
    {
        protected FishActivationData activationData = null;
        [SerializeField] protected AnimationCurve curve;
        [SerializeField] protected AnimationCurve curve2;
        [SerializeField] protected double[] An;
        [SerializeField] protected double[] Pn;

        /*protected float GetFx(double[] An, double[] Pn, float x, int level = 1)
        {
            var orderedAn = An.OrderByDescending(a=>a).ToList();

            var count = 0;
            var ret = 0f;
            for(int i = 0; i < An.Length && count++ < level+1; ++i)
            {
                var an = orderedAn[i];
                var id = An.ToList().IndexOf(an);
                var pn = Pn[id];

                if (math.abs(an) <= 0.01f) continue;
                ret += (float)(an * math.cos(id * x + pn));
            }
            return ret;
        }*/
        public SAProblem LoadData(string fileName = "SAProblem.data")
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            var ret = FileTool.Read<SAProblem>(path);
            //LogTool.Log("Loaded " + path);

            return ret;
        }
        protected void Start()
        {
            var sampleNum = 15;
            var inteval = new float2(0, 30);

            var sa = this.LoadData();
            this.activationData = (sa.Current as SAProblem.ActivationState.Data).ActivationData;

            this.activationData = FishActivationData.Load();

            var act = this.activationData.ToDiscreteFunctions()[0];
            /*for(var i = 0; i < act.SampleNum; ++i)
            {
                //act[i] = math.sin(2 * math.PI * i / (act.SampleNum-1) * 4);
                act[i] = 0;
            }

            act[3] = act[4] = 1;
            act[9] = act[10] = 1;*/

            this.curve = act.ToAnimationCurve();

            var vector = act.ToYVector();

            var array = vector.Select(s=>(double)s).ToArray();
            var dft = new DFT();

            dft.Initialize((uint)array.Length);

            // Call the DFT and get the scaled spectrum back
            Complex[] cSpectrum = dft.Execute(array);

            An = DSP.ConvertComplex.ToMagnitude(cSpectrum);
            Pn = DSP.ConvertComplex.ToPhaseRadians(cSpectrum);

            var maxAn = An.Max();
            var maxIndex = An.ToList().IndexOf(maxAn);
            var maxPn = Pn[maxIndex];

            var start = new Tuple<float, float>(inteval.x, 0);
            var end = new Tuple<float, float>(inteval.y, 0);
            var function = new X2FDiscreteFunction<float>(start, end, sampleNum);

            for (var i = 0; i < function.SampleNum; ++i)
            {
                var x = 2 * math.PI * i / (act.SampleNum - 1);
                //function[i] = (float)(An[0] / 2 + maxAn * math.cos(maxIndex * x + maxPn));
                //function[i] = FishActivationData.GetFx(An, Pn, x, 1);
            }

            this.curve2 = function.ToAnimationCurve();
        }
    }
}