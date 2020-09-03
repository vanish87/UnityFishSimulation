using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{
    public static class Matrix4x4e
    {
        public static readonly Matrix4x4 identity = Matrix4x4.identity;
    }
    public static class float3e
    {
        public static readonly float3 one = new float3(1, 1, 1);
    }
    public class WorldObject
    {
        public float size;
        public float3 position;
    }


    public class FishBody
    {
        public FishModelData modelData;

        public FishBody()
        {
            this.modelData = GeometryFunctions.Load();
        }
    }

    public class FishBrain
    {
        public Perception perception;
        public Habits habits;
        public MentalState mentalState;
        public PhysicalState physicalState;
        public IntensionGenerator intensionGenerator;
        public BehaviorSelector behaviorSelector;

        protected BehaviorRoutine current;

        public FishActivationData temp;

        public FishBrain(FishActivationData fishActivationData) : base()
        {
            this.temp = fishActivationData;
        }
        public FishBrain()
        {
            this.perception = new Perception();
            this.habits = new Habits();
            this.mentalState = new MentalState();
            this.physicalState = new PhysicalState();
            this.intensionGenerator = new IntensionGenerator();
            this.behaviorSelector = new BehaviorSelector();

            this.temp = FishActivationData.Load();
        }

        public void Update(float t)
        {
            this.UpdateSensor(t);
            var intension = this.intensionGenerator.Generate(this.perception, this.habits, this.mentalState, this.physicalState, t);

            this.perception.focusser.Update(intension, this.perception);

            var behaviorRoutine = this.behaviorSelector.Generate(intension, this.perception);

            this.current = behaviorRoutine;
        }

        protected void UpdateSensor(float t)
        {
            this.perception.visionSensor.Scan();
        }  
    }
    public class VisionSensor
    {
        public float3 Position => this.position;

        [SerializeField] protected float3 forward = float3e.one;
        [SerializeField] protected float3 position = float3.zero;
        [SerializeField] protected int angle = 360;
        [SerializeField] protected int Vr = 1; // visual range
        [SerializeField] protected int Or = 2; //foraging range

        [SerializeField] protected List<WorldObject> currentVisiableObjects;

        public void Scan()
        {

        }

        public void OnGizmo()
        {
            using(new GizmosScope(Color.white, Matrix4x4.identity))
            {
                Gizmos.DrawWireSphere(this.position, this.Vr);
                Gizmos.DrawWireSphere(this.position, this.Or);
            }
        }
    }

    public class TemperatureSensor
    {

    }

    public class SensorData
    {
        [SerializeField] protected float worldBrightness = 1;
    }
    
    public class Focusser
    {
        public class CollisionInfo
        {
            public WorldObject cloesetObj;
            public List<WorldObject> collisions;
        }
        public class Target
        {
            public float3 position;

            public CollisionInfo collisionInfo;
        }
        //Intension->MotorPreference->Focus
        public class MotorPreference
        {
            public enum Type
            {
                TurnLeft,
                TurnRight,
                MoveForawrd
            }
        }
        public Target target;
        //active focus and filter out none-important sensor data
        //save to Target
        public Target Update(Intension intension, Perception perception)
        {
            //calculate desires
            //avoid, fear, eat, mate

            //if intension == avoid

            //if intension == escape

            return default;
        }
    }

    public class Perception
    {
        public VisionSensor visionSensor;
        public TemperatureSensor temperatureSensor;
        public SensorData sensorData;

        public Focusser focusser;

        public Perception()
        {
            this.visionSensor = new VisionSensor();
            this.temperatureSensor = new TemperatureSensor();
            this.sensorData = new SensorData();
            this.focusser = new Focusser();
        }

        public Focusser.CollisionInfo GetCollisions() { return this.focusser.target.collisionInfo; } 
    }
    public class Habits
    {
        public enum LightPerference
        {
            Brightness,
            Neutral,
            Darkness,
        }
        public enum TempratruePerference
        {
            Cold,
            Neutral,
            Warmth,
        }

        public LightPerference light = LightPerference.Neutral;
        public TempratruePerference temprature = TempratruePerference.Neutral;
        public bool isMale = true;
        public bool schooling = true;
    }
    public class MentalState
    {
        public float hunger;
        public float libido;
        public float fear;

        protected float S(Focusser.Target target, float q1, float q2)
        {
            var current = float3.zero;
            var s = 1 / math.length(current - target.position);

            if(s < q1)
            {
                return 0;
            }
            else
            if(q1 <= s && s <= q2)
            {
                return (s - q1) / (2 * q2 - q1 - s);
            }
            else
            {
                return 1;
            }
        }

        public float Sh(Focusser.Target target) { return this.S(target, 0.05f, 0.2f); }
        public float Sl(Focusser.Target target) { return this.S(target, 0.025f, 0.1f); }

        public float H(float t, PhysicalState ps, Focusser.Target target)
        {
            var internalUrge = 1 - ps.foodEaten * ps.Rx(ps.timeSinceLastMeal) / ps.appetite;
            var externalStimuli = ps.alphah * this.Sh(target);

            return math.min(internalUrge + externalStimuli, 1);
        }

        public float L(float t, PhysicalState ps, Focusser.Target target)
        {
            var Ht = H(t, ps, target);
            var internalUrge = ps.Lx(ps.timeSinceLastMating) * (1 - Ht);
            var externalStimuli = this.Sl(target);

            return math.min(internalUrge + externalStimuli, 1);
        }

        public float F(float t, PhysicalState ps, Focusser.Target target)
        {
            var ret = 0;
            var internalUrge = 0;
            //var externalStimuli =

            return math.min(1, ret);
        }
    }

    public class PhysicalState
    {
        public readonly float p_0 = 0.00067f;// Digestion rate
        public readonly float alphah = 0.8f;

        public int foodEaten = 0;
        public float timeSinceLastMeal = 0;
        public float appetite = 1;
        public float Rx(float x) { return 1 - p_0 * x; }


        public readonly float p_1 = 0.0025f;// libido
        public readonly float alphal = 0.5f;

        public float timeSinceLastMating = 0;
        public float Lx(float x) { return p_1 * x; }

        public float D_0 = 200;//fear constant (D_0=500 => cowards)
        public float Fi(float di) { return math.min(D_0 / di, 1); }
        
        public void UpdateTime(float t)
        {
            this.timeSinceLastMeal += t;
            this.timeSinceLastMating += t;
        }
        public void Eat(int foodNum)
        {
            this.foodEaten += foodNum;
        }
    }
    public abstract class Intension
    {
        public enum Type
        {
            Avoid,
            Escape,
            School,
            Eat,
            Mate,
            Leave,
            Wander
        }

        public abstract Type IntensionType { get; }
    }

    public class AvoidIntension : Intension
    {
        public override Type IntensionType => Type.Avoid;
        public AvoidIntension(WorldObject cloesetObj)
        {

        }
    }
    public class EscapdeIntension : Intension
    {
        public override Type IntensionType => Type.Escape;
        public EscapdeIntension(WorldObject cloesetObj)
        {

        }
    }
    public class SchoolIntension : Intension
    {
        public override Type IntensionType => Type.School;
        public SchoolIntension()
        {

        }
    }
    public class EatIntension : Intension
    {
        public override Type IntensionType => Type.Eat;
        public EatIntension()
        {

        }
    }
    public class MateIntension : Intension
    {
        public override Type IntensionType => Type.Mate;
        public MateIntension()
        {

        }
    }
    public class WanderIntension : Intension
    {
        public override Type IntensionType => Type.Wander;
        public WanderIntension()
        {

        }
    }
    public class IntensionGenerator
    {
        public float f0 = 0.5f;//[0.1,0.5]
        public float f1 = 0.8f;//f1 > f0
        public float r = 0.25f;//[0,0.5]

        public LinkedList<Intension> intensionStack;
        public Intension Generate(Perception perception, Habits habits, MentalState mental, PhysicalState ps, float t)
        {
            return default;
            Intension intension = default;
            var collisions = perception.GetCollisions();
            var distance = collisions.cloesetObj == null ? -1 : math.distance(collisions.cloesetObj.position, perception.visionSensor.Position);
            var dangerDistance = 10;
            if(distance > 0 && distance < dangerDistance)
            {
                intension = new AvoidIntension(collisions.cloesetObj);
            }
            else
            {
                //most dangerous predator m
                var Fm = distance;// collisions.cloesetObj;
                var F = mental.F(t, ps, perception.focusser.target);
                if (F > this.f0)
                {
                    if(Fm < this.f1 && habits.schooling)
                    {
                        intension = new SchoolIntension();
                    }
                    else
                    {
                        intension = new EscapdeIntension(collisions.cloesetObj);
                    }
                }
                else
                {
                    if (this.IsLastEatOrMate())
                    {
                        intension = this.intensionStack.Last.Value;
                        this.intensionStack.RemoveLast();
                    }
                    else
                    { 
                        intension = this.GenerateNewIntension(perception, habits, mental, ps, t);
                    }
                }
            }

            if (this.intensionStack.Count == 0 || this.intensionStack.Last.Value.IntensionType != Intension.Type.Avoid)
            {
                this.intensionStack.AddLast(intension);
            }

            if (this.intensionStack.Count > 1) this.intensionStack.RemoveFirst();

            return intension;
        }

        protected bool IsLastEatOrMate()
        {
            if (this.intensionStack.Count == 0) return false;

            var It1 = this.intensionStack.Last.Value;
            return (It1.IntensionType == Intension.Type.Eat || It1.IntensionType == Intension.Type.Mate);
        }

        protected Intension GenerateNewIntension(Perception perception, Habits habits, MentalState mental, PhysicalState ps, float t)
        {
            Intension intension = default;
            var H = mental.H(t, ps, perception.focusser.target);
            var L = mental.L(t, ps, perception.focusser.target);

            if (this.r < math.max(H, L))
            {
                if (H == L)
                {
                    if (ThreadSafeRandom.NextFloat() > 0.5f) intension = new EatIntension();
                    else intension = new MateIntension();
                }
                else
                {
                    if (H > L) intension = new EatIntension();
                    else intension = new MateIntension();
                }
            }
            else
            {
                //perception.temperatureSensor;
                //perception.visionSensor;

                //generate wonder or leave
                intension = new WanderIntension();
            }

            return intension;
        }
    }

    public class BehaviorSelector
    {
        public BehaviorRoutine Generate(Intension intension, Perception perception)
        {
            var ret = new List<MotorController>();
            //MC logical
            var behavior = new ChasingTarget();
            

            return behavior;
        }
    }

    public abstract class BehaviorRoutine
    {
        public abstract List<MotorController> ToMC();
        public abstract void Init(Intension intension, Perception perception);
    }

    public class ObstacleAvoidance : BehaviorRoutine
    {
        public override void Init(Intension intension, Perception perception)
        {

            //sensory information 
            //motor preferences
            //=> MC and MC parameters
        }

        public override List<MotorController> ToMC()
        {
            return default;
        }
    }

    public class ChasingTarget : BehaviorRoutine
    {
        public override void Init(Intension intension, Perception perception)
        {

            //sensory information 
            //motor preferences
            //=> MC and MC parameters
        }

        public override List<MotorController> ToMC()
        {
            return default;
        }
    }

    public class Wandering : BehaviorRoutine
    {        
        public override void Init(Intension intension, Perception perception)
        {

        }

        public override List<MotorController> ToMC()
        {
            var ret = new List<MotorController>();
            ret.Add(new SwimMC());
            return ret;
        }
    }

    public class MotorController
    {

    }

    public abstract class MuscleMC : MotorController
    {
        public class Parameter
        {
            public readonly float2 aMinMax = new float2(0, 1);
            public readonly float2 fMinMax = new float2(0, 1);
            public float amplitude;
            public float frequency;
        }
        protected FishActivationData activationData;
        protected Dictionary<Spring.Type, Parameter> muscleControlParamters;
        protected abstract List<Spring.Type> GetSprtingTypes();
        protected abstract string FileName { get; }

        public MuscleMC()
        {
            this.muscleControlParamters = new Dictionary<Spring.Type, Parameter>();
            var types = this.GetSprtingTypes();
            foreach(var t in types)
            {
                this.muscleControlParamters.Add(t, new Parameter());
                this.muscleControlParamters.Add(t, new Parameter());
            }

            this.activationData = FishActivationData.Load(this.FileName);
        }
        protected virtual Parameter GetParameter(Spring.Type type) { return default; }
    }

    public class SwimMC : MuscleMC
    {
        protected float speed = 1;
        protected override string FileName => "Swimming";
        protected override List<Spring.Type> GetSprtingTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
        }
        protected override Parameter GetParameter(Spring.Type type)
        {
            //convert from speed to parameter
            return default;
        }

    }

    public class TurnMC : MuscleMC
    {
        protected Dictionary<int, Parameter> turnAnlgeMap;
        protected float angle = 0;
        protected override string FileName => "Turn";
        protected override List<Spring.Type> GetSprtingTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle };
        }

        protected override Parameter GetParameter(Spring.Type type)
        {
            //convert from speed to parameter
            return default;
        }
        public TurnMC() : base()
        {
            this.turnAnlgeMap = new Dictionary<int, Parameter>();
        }
    }

    public class GlideMC : MuscleMC
    {
        protected float time = 0;
        protected override string FileName => "Glide";
        protected override List<Spring.Type> GetSprtingTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
        }

        protected override Parameter GetParameter(Spring.Type type)
        {
            //convert from speed to parameter
            return default;
        }
    }

    public class BalanceMC : MotorController
    {
        protected float3 worldUp = new float3(0, 1, 0);
        protected float leftFin;
        protected float rightFin;
    }
}