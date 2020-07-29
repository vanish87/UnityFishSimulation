using System;
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
    /*

    public class Simulator
    {
        [SerializeField] protected FishModelData fish;
        [SerializeField] protected FishSolver solver;

        [SerializeField] protected DiscreteFunction trajectory = new DiscreteFunction();

        public void Init(FishModelData fish, FishSolver solver)
        {
            this.fish = fish;
            this.solver = solver;
        }
    }*/

    public class DiscreteFunction<XValue, YValue>
    {
        [SerializeField] protected CricleData<List<Tuple<XValue,YValue>>, int> valueMap;
        [SerializeField] protected Tuple<XValue, YValue> start;
        [SerializeField] protected Tuple<XValue, YValue> end;
        [SerializeField] protected int sampleNum;

        protected virtual void InitValues()
        {
            this.AddValue(this.start);
            for (var i = 1; i < this.sampleNum - 1; ++i)
            {
                this.AddValue(new Tuple<XValue, YValue>(default, default));
            }
            this.AddValue(this.end);

            LogTool.LogAssertIsTrue(this.valueMap.Current.Count == this.valueMap.Next.Count && this.valueMap.Current.Count == sampleNum, "Sample size inconstant");
        }

        public DiscreteFunction(Tuple<XValue, YValue> start, Tuple<XValue, YValue> end, int sampleNum)
        {
            LogTool.LogAssertIsTrue(sampleNum > 0, "Sample size should none 0");

            if (start == null) start = new Tuple<XValue, YValue>(default, default);
            if (end == null) end = new Tuple<XValue, YValue>(default, default);

            this.valueMap = new CricleData<List<Tuple<XValue, YValue>>, int>(2);
            this.start = start;
            this.end = end;
            this.sampleNum = sampleNum;

            this.InitValues();
        }
        public void MoveToNext()
        {
            this.valueMap.MoveToNext();
        }
        public void MoveToPrev()
        {
            this.valueMap.MoveToPrev();
        }
        public XValue GetValueX(int index)
        {
            var x = math.clamp(index, 0, this.valueMap.Current.Count - 1);
            return this.valueMap.Current[x].Item1;
        }

        public void SetValueY(int index, YValue value)
        {
            var x = math.clamp(index, 0, this.valueMap.Current.Count - 1);
            var old = this.valueMap.Current[x];
            this.valueMap.Current[x] = new Tuple<XValue, YValue>(old.Item1, value);
        }

        public YValue EvaluateIndex(int index)
        {
            var x = math.clamp(index, 0, this.valueMap.Current.Count - 1);
            return this.valueMap.Current[x].Item2;
        }

        protected void AddValue(Tuple<XValue, YValue> value)
        {
            this.valueMap.Current.Add(value);
            this.valueMap.Next.Add(value);
        }
    }

    public class F2XDiscreteFunction<Y> : DiscreteFunction<float, Y>
    {
        public F2XDiscreteFunction():base(default,default,1)
        {

        }
        public F2XDiscreteFunction(Tuple<float, Y> start, Tuple<float, Y> end, int sampleNum):base(start, end, sampleNum)
        {

        }
        protected override void InitValues()
        {
            var h = (this.end.Item1 - this.start.Item1)/this.sampleNum;
            this.AddValue(this.start);
            for (var i = 1; i < this.sampleNum - 1; ++i)
            {
                this.AddValue(new Tuple<float, Y>(this.start.Item1 + i * h, default));
            }
            this.AddValue(this.end);

            LogTool.LogAssertIsTrue(this.valueMap.Current.Count == this.valueMap.Next.Count && this.valueMap.Current.Count == sampleNum, "Sample size inconstant");

        }

        public virtual Y Lerp(Y from, Y to, float t) { return default; }

        public Y Evaluate(float t)
        {
            var range = this.end.Item1 - this.start.Item1;
            LogTool.LogAssertIsFalse(range == 0, "range is 0");

            if (range == 0) return default;
            var h = range / this.valueMap.Current.Count;

            var index = (t % range) / h;
            var from = Mathf.FloorToInt(index);
            var to = Mathf.CeilToInt(index);
            var yfrom = this.EvaluateIndex(from);
            var yto = this.EvaluateIndex(to);

            return this.Lerp(yfrom, yto, index - from);
        }
    }

    public class F2FloatDiscreteFunction:F2XDiscreteFunction<float>
    {

        public F2FloatDiscreteFunction(Tuple<float, float> start, Tuple<float, float> end, int sampleNum) : base(start, end, sampleNum)
        {

        }
        public override float Lerp(float from, float to, float t)
        {
            return math.lerp(from, to, t);
        }
        public void RandomNextValues()
        {
            for(var i = 0; i < this.valueMap.Next.Count; ++i)
            {
                var n = this.valueMap.Next[i].Item1;
                this.valueMap.Next[i] = new Tuple<float, float>(n, UnityEngine.Random.value);
            }
        }
        public float Devrivate(int index, float h)
        {
            return (this.EvaluateIndex(index - 1) + this.EvaluateIndex(index + 1)) / (2 * h);
        }
        public float Devrivate2(int index, float h)
        {
            return (this.EvaluateIndex(index - 1) + this.EvaluateIndex(index + 1) - (2 * this.EvaluateIndex(index))) / (h * h);
        }
        public void OnGizmos(float3 offset)
        {
            if (this.valueMap != null)
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
            }
        }
    }
    public class F2Float2DiscreteFunction : F2XDiscreteFunction<float2>
    {

        public F2Float2DiscreteFunction(Tuple<float, float2> start, Tuple<float, float2> end, int sampleNum) : base(start, end, sampleNum)
        {

        }
        public override float2 Lerp(float2 from, float2 to, float t)
        {
            return math.lerp(from, to, t);
        }
    }
    public class F2Float3DiscreteFunction : F2XDiscreteFunction<float3>
    {

        public F2Float3DiscreteFunction(Tuple<float, float3> start, Tuple<float, float3> end, int sampleNum) : base(start, end, sampleNum)
        {

        }
        public override float3 Lerp(float3 from, float3 to, float t)
        {
            return math.lerp(from, to, t);
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
            SADone,
        }


        [SerializeField] protected FishModelData fishData = new FishModelData();
        [SerializeField] protected FishEularSolver fishSolver = new FishEularSolver();

        [SerializeField] protected Dictionary<Spring.Type, F2FloatDiscreteFunction> activations = new Dictionary<Spring.Type, F2FloatDiscreteFunction>();
        [SerializeField] protected F2Float3DiscreteFunction trajectory;

        [SerializeField] protected float2 timeInterval = new float2(0, 5);
        [SerializeField] protected int sampleSize = 15;
        [SerializeField] protected float h;

        [SerializeField] protected float temperature = 1;
        [SerializeField] protected float minTemperature = 0.0001f;
        [SerializeField] protected float alpha = 0.99f;


        [SerializeField, Range(0, 1)] protected float act = 0.5f;

        [SerializeField] protected float current = 0;
        [SerializeField] protected int currentTrajactory = 0;
        [SerializeField] protected SAState currentState = SAState.Start;

        protected void Start()
        {
            this.activations.Clear();

            this.h = (this.timeInterval.y - this.timeInterval.x) / sampleSize;

            var start = new Tuple<float, float>(this.timeInterval.x, 0.5f);
            var end   = new Tuple<float, float>(this.timeInterval.y, 0.5f);


            this.activations.Add(Spring.Type.MuscleFront, new F2FloatDiscreteFunction(start, end, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleMiddle, new F2FloatDiscreteFunction(start, end, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleBack, new F2FloatDiscreteFunction(start, end, this.sampleSize));

            this.trajectory = new F2Float3DiscreteFunction(
                new Tuple<float, float3>(this.timeInterval.x, 0), 
                new Tuple<float, float3>(this.timeInterval.y, 0), 
                this.sampleSize);

            this.fishData = GeometryFunctions.Load();            
        }

        protected void Update()
        {
            switch(this.currentState)
            {
                case SAState.Start:
                    {
                        this.current = 0;
                        this.fishData = GeometryFunctions.Load();

                        this.currentTrajactory = 0;
                        this.trajectory = new F2Float3DiscreteFunction(
                            new Tuple<float, float3>(this.timeInterval.x, 0),
                            new Tuple<float, float3>(this.timeInterval.y, 0),
                            this.sampleSize);

                        this.temperature = 1;

                        this.currentState = SAState.FishTrajectorySimulating;
                    }
                    break;
                case SAState.FishTrajectorySimulating:
                    {
                        var end = this.timeInterval.y;
                        if (this.current < end)
                        {
                            this.ApplyControlParameter(this.fishData, this.current);

                            this.Simulate();
                            this.current += Solver.dt;

                            if(this.current > this.trajectory.GetValueX(this.currentTrajactory))
                            {
                                var dis = math.length(new float3(100, 0, 0) - this.fishData.Head.Position);
                                this.trajectory.SetValueY(this.currentTrajactory, dis);
                                this.currentTrajactory++;
                            }
                        }
                        else
                        {
                            this.currentState = SAState.SAEvaluating;
                        }
                    }
                    break;
                case SAState.SAEvaluating:
                    {
                        this.StepSimulatedAnnealing();
                        if (this.temperature <= this.minTemperature)
                        {
                            this.currentState = SAState.SADone;
                        }
                        else 
                        {
                            this.currentState = SAState.Start;
                        }
                    }
                    break;
                default:break;
            }


            if(Input.GetKeyDown(KeyCode.A))
            {
                this.ApplyControlParameter(this.fishData, this.act);
                this.Simulate();
            }
        }

        protected void Simulate()
        {
            this.fishSolver.Step(this.fishData, Solver.dt);
        }

        protected void SimulateNext()
        {
            this.trajectory.MoveToNext();
            this.fishData = GeometryFunctions.Load();

            var s = this.timeInterval.x;
            var e = this.timeInterval.y;
            var c = 0;

            while(s < e)
            {
                this.ApplyControlParameter(this.fishData, s);
                this.fishSolver.Step(this.fishData, Solver.dt);
                s += Solver.dt;

                if (s > this.trajectory.GetValueX(c))
                {
                    var dis = math.length(new float3(100, 0, 0) - this.fishData.Head.Position);
                    this.trajectory.SetValueY(c, dis);
                    c++;
                }
            }
        }

        protected void ApplyControlParameter(FishModelData fish, float current)
        {
            this.ApplyByType(fish, Spring.Type.MuscleFront, current);
            this.ApplyByType(fish, Spring.Type.MuscleMiddle, current);
            this.ApplyByType(fish, Spring.Type.MuscleBack, current);
        }
        protected void ApplyByType(FishModelData fish, Spring.Type type, float t)
        {
            var muscle = fish.GetSpringByType(new List<Spring.Type>() { type });
            var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
            var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);
            var activation = this.activations[type];


            foreach (var l in muscleLeft)
            {
                //l.Activation = act;
                //l.Activation = cos;// 
                l.Activation = activation.Evaluate(t);
            }
            foreach (var r in muscleRight)
            {
                //r.Activation = 1 - act;
                //r.Activation = 1 - cos;// 
                r.Activation = 1 - activation.Evaluate(t);
            }

        }

        protected void StepSimulatedAnnealing()
        {
            var current = this.GetCurrentE();
            if (this.temperature > this.minTemperature)
            {
                foreach (var a in this.activations.Values)
                {
                    a.RandomNextValues();
                    a.MoveToNext();
                }

                this.SimulateNext();

                var next = this.GetCurrentE();
                if (this.ShouldUseNext(current, next) == false)
                {
                    foreach (var a in this.activations.Values)
                    {
                        a.MoveToPrev();
                    }
                }

                //this.temperature = this.temperature/(1+this.alpha*this.temperature);
                this.temperature *= this.alpha;

                Debug.Log("Current " + current + " Next " + next);
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
            var mu2 = 1f;

            var E = 0f;

            for (int i = 0; i < this.sampleSize; ++i)
            {
                var Eu = 0f;
                var Ev = math.length(this.trajectory.Evaluate(i) - new float3(100,0,0));

                var v1 = 1f;
                var v2 = 1f;

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
                    activation.OnGizmos(offset);
                    offset.y += 3;
                }

                //this.trajectory.OnGizmos(offset);


                var act = this.activations[Spring.Type.MuscleFront];
                drawtestY = act.Evaluate(drawtestX);
                Gizmos.DrawSphere(new Vector3(drawtestX + 10, drawtestY), 0.1f);
            }

            this.fishData.OnGizmos(GeometryFunctions.springColorMap);
        }

    }

    public class SwimMotorController : MotorController
    {
        [SerializeField] protected float speed;


    }
}
