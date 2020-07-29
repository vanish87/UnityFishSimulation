using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{/*

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
    public class MotorController : MonoBehaviour
    {
        [System.Serializable]
        public class DiscreteFunction
        {
            [SerializeField] protected CricleData<List<float2>, int> valueMap;
            public DiscreteFunction(float2 start, float2 end, float h, int sampleSize)
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

            public void SetValueY(int index, float value)
            {
                var x = math.clamp(index, 0, this.valueMap.Current.Count - 1);
                var old = this.valueMap.Current[x];
                this.valueMap.Current[x] = new float2(old.x, value);
            }

            public float GetValueX(int index)
            {
                var x = math.clamp(index, 0, this.valueMap.Current.Count - 1);
                return this.valueMap.Current[x].x;
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
            public void OnGizmos(float3 offset)
            {
                if (this.valueMap != null)
                {
                    using (new GizmosScope(Color.cyan, Matrix4x4.TRS(new Vector3(offset.x, offset.y, offset.z), Quaternion.identity, Vector3.one)))
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

        [SerializeField] protected Dictionary<Spring.Type, DiscreteFunction> activations = new Dictionary<Spring.Type, DiscreteFunction>();
        [SerializeField] protected DiscreteFunction trajectory;

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

            var start = new float2(this.timeInterval.x, 0.5f);
            var end   = new float2(this.timeInterval.y, 0.5f);


            this.activations.Add(Spring.Type.MuscleFront, new DiscreteFunction(start, end, this.h, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleMiddle, new DiscreteFunction(start, end, this.h, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleBack, new DiscreteFunction(start, end, this.h, this.sampleSize));

            this.trajectory = new DiscreteFunction(start, end, this.h, this.sampleSize);

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
                        this.trajectory = new DiscreteFunction(float2.zero, float2.zero, this.h, this.sampleSize);

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
                l.Activation = activation.Evaluate(t, this.timeInterval);
            }
            foreach (var r in muscleRight)
            {
                //r.Activation = 1 - act;
                //r.Activation = 1 - cos;// 
                r.Activation = 1 - activation.Evaluate(t, this.timeInterval);
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
                var Ev = this.trajectory.Evaluate(i);

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

                this.trajectory.OnGizmos(offset);


                var act = this.activations[Spring.Type.MuscleFront];
                drawtestY = act.Evaluate(drawtestX, this.timeInterval);
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
