using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishBrain : MonoBehaviour
    {
        public Intension CurrentIntension => this.currentIntension;
        public Desire CurrentDesire => this.currentDesire;
        [SerializeField] protected Habits habits = new Habits();
        [SerializeField] protected MentalState mentalState = new MentalState();
        [SerializeField] protected PhysicalState physicalState = new PhysicalState();

        [SerializeField] protected IntensionGenerator intensionGenerator = new IntensionGenerator();

        [SerializeField] protected Intension currentIntension;
        [SerializeField] protected Desire currentDesire = new Desire();

        public void Init()
        {

        }
        public void UpdateBrain(FishSimulator.Delta delta, Perception perception)
        {
            var t = delta.deltaTime;
            this.physicalState.UpdateTime(t);
            this.mentalState.UpdateState(t, this.physicalState, perception.GetSensorData());

            this.currentIntension = this.intensionGenerator.Generate(perception, this.habits, this.mentalState, this.physicalState, t);
            this.currentDesire.UpdateDesire(this.mentalState, this.currentIntension);
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
    public class Desire
    {
        public float avoid = 0;
        public float fear = 0;
        public float eat = 0;
        public float mate = 0;
        public Intension.Type intensionType;

        public void UpdateDesire(MentalState mental, Intension intension)
        {
            this.intensionType = intension.IntensionType;

            var ti = 1f;
            switch(intension.IntensionType)
            {
                case Intension.Type.Escape: ti = mental.fear; break;
                case Intension.Type.Eat: ti = mental.hunger; break;
                case Intension.Type.Mate: ti = mental.libido; break;
                default: ti = 1; break;
            } 
            this.avoid = 1;
            this.fear = mental.fear / ti;
            this.eat = mental.hunger / ti;
            this.mate = mental.libido / ti;
        }
    }
    [System.Serializable]
    public class MentalState
    {
        //new ones
        public float hunger = 1;
        public float libido = 1;
        public float fear = 1;

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