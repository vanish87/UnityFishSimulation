using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishBrain : MonoBehaviour
    {

        [SerializeField] protected Habits habits = new Habits();
        [SerializeField] protected MentalState mentalState = new MentalState();
        [SerializeField] protected PhysicalState physicalState = new PhysicalState();

        protected Perception perception;
        protected IntensionGenerator intensionGenerator;

        protected LinkedList<BehaviorRoutine> behaviorRoutines = new LinkedList<BehaviorRoutine>();


        [SerializeField] protected Intension.Type currentIntension;

        [SerializeField] protected BehaviorRoutine currentBehavior;
        public void Init()
        {
            this.perception = this.GetComponentInChildren<Perception>();
            this.perception.Init();

            this.intensionGenerator = this.GetComponentInChildren<IntensionGenerator>();
        }

        public void UpdateBrain(float t)
        {
            this.perception.SensorUpdate(t);
            this.physicalState.UpdateTime(t);
            this.mentalState.UpdateState(t, this.physicalState, this.perception.GetSensorData());
            var intension = this.intensionGenerator.Generate(this.perception, this.habits, this.mentalState, this.physicalState, t);

            this.mentalState.UpdateDesire(t, this.physicalState, this.perception.GetSensorData(), intension);
            this.perception.UpdateFocusser(intension, this.mentalState);

            var behaviorRoutine = this.GenerateBehaviorRoutine(intension, this.perception);
            this.UpdateBehaviorRoutine(behaviorRoutine);


            this.currentIntension = intension.IntensionType;
        }

        public BehaviorRoutine GetBehaviorRoutine()
        {
            return this.behaviorRoutines.First.Value;
        }

        protected BehaviorRoutine GenerateBehaviorRoutine(Intension intension, Perception perception)
        {
            //MC logical
            this.currentBehavior = this.currentBehavior??new BehaviorRoutine();
            this.currentBehavior.Init(intension, perception);

            return this.currentBehavior;
        }

        protected void UpdateBehaviorRoutine(BehaviorRoutine routine)
        {

            if (this.behaviorRoutines.Count > 0)
            {
                //Update last one 
            }
            else
            {
                if (this.behaviorRoutines.Count < 10)
                {
                    this.behaviorRoutines.AddLast(routine);
                }
            }

        }

    }

    [System.Serializable]
    public class Habits
    {
        public enum LightPreference
        {
            Brightness,
            Neutral,
            Darkness,
        }
        public enum TempratruePreference
        {
            Cold,
            Neutral,
            Warmth,
        }

        public LightPreference light = LightPreference.Neutral;
        public TempratruePreference temperature = TempratruePreference.Neutral;
        public bool isMale = true;
        public bool schooling = true;
    }
    [System.Serializable]

    public class MentalState
    {
        //new ones
        public float hunger = 1;
        public float libido = 1;
        public float fear = 1;

        public float avoidDesire = 0;
        public float fearDesire = 0;
        public float eatDesire = 0;
        public float mateDesire = 0;

        protected float S(SensorObject target, float q1, float q2)
        {
            if(target == null) return 0;

            var s = 1 / target.distance;

            if (s < q1)
            {
                return 0;
            }
            else
            if (q1 <= s && s <= q2)
            {
                return (s - q1) / (2 * q2 - q1 - s);
            }
            else
            {
                return 1;
            }
        }

        public void UpdateState(float t, PhysicalState physical, SensorData sensor)
        {
            var closestFood = sensor.GetClosestByType(ObjectType.Food);
            var closestMate = sensor.GetClosestByType(ObjectType.Mate);
            this.H(t, physical, closestFood);
            this.L(t, physical, closestMate);
            this.F(t, physical, sensor.GetVisiable(ObjectType.Predator));
        }
        public void UpdateDesire(float t, PhysicalState physical, SensorData sensor, Intension intension)
        {
            var ti = 1f;
            switch(intension.IntensionType)
            {
                case Intension.Type.Escape: ti = this.fear; break;
                case Intension.Type.Eat: ti = this.hunger; break;
                case Intension.Type.Mate: ti = this.libido; break;
                default: ti = 1; break;
            } 
            this.avoidDesire = 1;
            this.fearDesire = this.fear / ti;
            this.eatDesire = this.hunger / ti;
            this.mateDesire = this.libido / ti;
        }
        private float Sh(SensorObject target) { return this.S(target, 0.05f, 0.2f); }
        private float Sl(SensorObject target) { return this.S(target, 0.025f, 0.1f); }

        private float H(float t, PhysicalState ps, SensorObject target)
        {
            var internalUrge = 1 - ps.foodEaten * ps.Rx(ps.timeSinceLastMeal) / ps.appetite;
            var externalStimuli = ps.alphah * this.Sh(target);


            var H = math.min(internalUrge + externalStimuli, 1);

            this.hunger = H;

            return this.hunger;
        }

        private float L(float t, PhysicalState ps, SensorObject target)
        {
            var Ht = this.hunger;
            var internalUrge = ps.Lx(ps.timeSinceLastMating) * (1 - Ht);
            var externalStimuli = this.Sl(target);

            var L = math.min(internalUrge + externalStimuli, 1);

            this.libido = L;

            return this.libido;
        }

        private float F(float t, PhysicalState ps, List<SensorObject> targets)
        {
            var externalStimuli = 0f;
            var internalUrge = 0;
            foreach(var p in targets)
            {
                externalStimuli += ps.Fi(p.distance); 
            }

            var F = math.min(internalUrge + externalStimuli, 1);

            this.fear = F;

            return this.fear;
        }
    }
    [System.Serializable]

    public class PhysicalState
    {
        public float p_0 = 0.00067f;// Digestion rate
        public float alphah = 0.8f;

        public int foodEaten = 0;
        public float timeSinceLastMeal = 0;
        public float appetite = 1;
        public float Rx(float x) { return 1 - p_0 * x; }


        public float p_1 = 0.0025f;// libido
        public float alphal = 0.5f;

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
            this.timeSinceLastMeal = 0;
        }
    }
}