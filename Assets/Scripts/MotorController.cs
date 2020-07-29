using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{
    public class MotorController : MonoBehaviour
    {
        [System.Serializable]
        public class MuscleActuatorControlFunction
        {
            [SerializeField] protected CricleData<List<float2>, int> valueMap;
            public MuscleActuatorControlFunction(float2 start, float2 end, float h, int sampleSize)
            {
                LogTool.LogAssertIsTrue(sampleSize > 0, "Sample size should none 0");
                this.valueMap = new CricleData<List<float2>, int>(2);

                this.AddValue(start);
                for(var i = 1; i < sampleSize-1; ++i)
                {
                    this.AddValue(new float2(start.x + h * i, UnityEngine.Random.value));
                }
                this.AddValue(end);

                LogTool.LogAssertIsTrue(this.valueMap.Current.Count == this.valueMap.Next.Count && this.valueMap.Current.Count == sampleSize, "Sample size inconstant");
            }
            public void RandomNextValues(bool withStartEnd = false)
            {
                var s = withStartEnd ? 0:1;
                var e = withStartEnd ? this.valueMap.Next.Count : this.valueMap.Next.Count-1;
                for (var i = s; i < e; ++i)
                {
                    this.valueMap.Next[i] = new float2(this.valueMap.Next[i].x, UnityEngine.Random.value);
                }
            }
            public void MoveToNext()
            {
                this.valueMap.MoveToNext();
            }
            public void MoveToPrev()
            {
                this.valueMap.MoveToPrev();
            }
            public float Evaluate(float t, float2 minMax)
            {
                var range = minMax.y - minMax.x;
                var h = range / this.valueMap.Current.Count;

                var index = (t % minMax.y) / h;
                var from = Mathf.FloorToInt(index);
                var to = Mathf.CeilToInt(index);
                var yfrom = this.Evaluate(from);
                var yto = this.Evaluate(to);

                return math.lerp(yfrom, yto, index-from);
            }

            public float Evaluate(int index)
            {
                var x = math.clamp(index, 0, this.valueMap.Current.Count - 1);
                return this.valueMap.Current[x].y;
            }

            public float Devrivate(int index, float h)
            {
                return (this.Evaluate(index - 1) + this.Evaluate(index + 1)) / (2 * h);
            }
            public float Devrivate2(int index, float h)
            {
                return (this.Evaluate(index - 1) + this.Evaluate(index + 1) - (2 * this.Evaluate(index))) / (h * h);
            }
            public void OnGizmos()
            {
                if (this.valueMap != null)
                {
                    using (new GizmosScope(Color.cyan, Matrix4x4.TRS(new Vector3(10, 0, 0), Quaternion.identity, Vector3.one)))
                    {
                        for (var i = 1; i < this.valueMap.Current.Count; ++i)
                        {
                            var from = new Vector3(this.valueMap.Current[i - 1].x, this.valueMap.Current[i - 1].y);
                            var to = new Vector3(this.valueMap.Current[i].x, this.valueMap.Current[i].y);
                            Gizmos.DrawLine(from, to);
                        }
                    }
                }
            }

            protected void AddValue(float2 value)
            {
                this.valueMap.Current.Add(value);
                this.valueMap.Next.Add(value);
            }
        }

        public const float Smax = 0.075f;

        [SerializeField] protected StructureModel fishModel;
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

        [SerializeField] protected List<MuscleActuatorControlFunction> activations = new List<MuscleActuatorControlFunction>();

        [SerializeField] protected float2 timeInterval = new float2(0, 5);
        [SerializeField] protected int sampleSize = 15;
        [SerializeField] protected float h;

        [SerializeField] protected float temperature = 1;
        [SerializeField] protected float minTemperature = 0.0001f;
        [SerializeField] protected float alpha = 0.98f;

        [SerializeField, Range(0.5f, 10)] protected float phaseScale = 0.5f;

        protected void Start()
        {
            var num = 3;// 12;
            this.activations.Clear();

            this.h = (this.timeInterval.y - this.timeInterval.x) / sampleSize;

            var start = new float2(this.timeInterval.x, 0.5f);
            var end   = new float2(this.timeInterval.y, 0.5f);
            for (var i = 0; i < num; ++i)
            {
                this.activations.Add(new MuscleActuatorControlFunction(start, end, this.h, this.sampleSize));
            }
        }

        protected void Update()
        {
            //if(Input.GetKey(KeyCode.N))
            {
                this.StepSimulatedAnnealing();
            }

            //if(Input.GetKey(KeyCode.M))
            {
                this.ApplyToMuscle();
            }
        }

        public float currentT = 0;
        [Range(0,1)]public float act = 0.5f;
        protected void ApplyByType(Spring.Type type)
        {
            var muscle = this.fishModel.GetSpringByType(new List<Spring.Type>() { type });
            var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
            var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);
            var activation = this.activations[0];


            //var phase = 2 * math.PI;
            var phase = timeInterval.y - timeInterval.x;

            this.currentT += 0.055f;
            this.currentT %= phase;

            var t = this.currentT;
            t = (t + (type == Spring.Type.MuscleBack ? math.PI : 0)) % phase;


            var cos = (1 - (math.cos(t) + 1) * 0.5f);

            foreach (var l in muscleLeft)
            {
                //l.Activation = act;
                //l.Activation = cos;// 
                l.Activation = activation.Evaluate(t, this.timeInterval);
            }
            foreach (var r in muscleRight)
            {
                //r.Activation = 1 - act;
                //r.Activation = 1 - cos;// 
                r.Activation = 1 - activation.Evaluate(t, this.timeInterval);
            }

        }

        protected void ApplyToMuscle()
        {
            this.ApplyByType(Spring.Type.MuscleBack);
            this.ApplyByType(Spring.Type.MuscleMiddle);
        }

        protected void StepSimulatedAnnealing()
        {
            var count = 0;
            var current = this.GetCurrentE();
            while (this.temperature > this.minTemperature)
            {
                count++;
                foreach (var a in this.activations)
                {
                    a.RandomNextValues();
                    a.MoveToNext();
                }

                var next = this.GetCurrentE();
                if (this.ShouldUseNext(current, next) == false)
                {
                    foreach (var a in this.activations)
                    {
                        a.MoveToPrev();
                    }
                }

                //this.temperature = this.temperature/(1+this.alpha*this.temperature);
                this.temperature *= this.alpha;

                //Debug.Log("Current " + current + " Next " + next);
            }
            
            {
                //Debug.Log("Current " + current);
                //Debug.Log("Min temp reached with count " + count);
                if (current > 1)
                {
                    this.temperature = 1;
                }
                else
                {
                    LogTool.Log("Start motor");
                }
            }

        }

        protected bool ShouldUseNext(float current, float next)
        {
            if (next < current)
            {
                return true;
            }
            else
            {
                var p = math.pow(math.E, -(next - current) / this.temperature);
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
            var mu1 = 1f;
            var mu2 = 0f;

            var E = 0f;

            for (int i = 0; i < this.sampleSize; ++i)
            {
                var Eu = 0f;
                var Ev = 0f;

                var v1 = 1f;
                var v2 = 1f;

                var du = 0f;
                var du2 = 0f;
                foreach (var fun in this.activations)
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
                this.activations[0].OnGizmos();

                drawtestY = this.activations[0].Evaluate(drawtestX, this.timeInterval);
                Gizmos.DrawSphere(new Vector3(drawtestY + 10, drawtestY), 0.1f);
            }
        }

    }

    public class SwimMotorController : MotorController
    {
        [SerializeField] protected float speed;


    }
}
