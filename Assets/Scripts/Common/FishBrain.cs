using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishBrain : MonoBehaviour
    {
        public Habits habits = new Habits();
        public MentalState mentalState = new MentalState();
        public PhysicalState physicalState = new PhysicalState();

        public Perception perception;
        public IntensionGenerator intensionGenerator;

        protected LinkedList<BehaviorRoutine> behaviorRoutines = new LinkedList<BehaviorRoutine>();

        public void Init()
        {
            this.perception = this.GetComponentInChildren<Perception>();
            this.perception.Init();

            this.intensionGenerator = this.GetComponentInChildren<IntensionGenerator>();
        }

        public void UpdateBrain(float t)
        {
            this.perception.SensorUpdate(t);
            this.mentalState.Update(t, this.physicalState, this.perception.GetSensorData());
            var intension = this.intensionGenerator.Generate(this.perception, this.habits, this.mentalState, this.physicalState, t);

            this.perception.focusser.Update(intension, this.perception, this.mentalState);

            var behaviorRoutine = this.GenerateBehaviorRoutine(intension, this.perception);
            this.UpdateBehaviorRoutine(behaviorRoutine);
        }

        public BehaviorRoutine GetBehaviorRoutine()
        {
            return this.behaviorRoutines.First.Value;
        }

        protected BehaviorRoutine GenerateBehaviorRoutine(Intension intension, Perception perception)
        {
            var ret = new List<MotorController>();
            //MC logical
            var behavior = new ChasingTarget();
            behavior.Init(intension, perception);

            return behavior;
        }

        protected void UpdateBehaviorRoutine(BehaviorRoutine routine)
        {

            if (this.behaviorRoutines.Count > 0 && this.behaviorRoutines.Last.Value is ChasingTarget)
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
        public TempratruePerference temperature = TempratruePerference.Neutral;
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

        //desires 
        public float ta = 1;
        public float tf;
        public float te;
        public float tm;

        protected float S(ISensorableObject target, float q1, float q2)
        {
            var current = new float3(0, 0, 0);
            var s = 1 / math.length(current - target.Position);

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

        public void Update(float t, PhysicalState physical, SensorData sensor)
        {
            //this.H(t, ps, )        
        }
        private float Sh(ISensorableObject target) { return this.S(target, 0.05f, 0.2f); }
        private float Sl(ISensorableObject target) { return this.S(target, 0.025f, 0.1f); }

        private float H(float t, PhysicalState ps, ISensorableObject target)
        {
            var internalUrge = 1 - ps.foodEaten * ps.Rx(ps.timeSinceLastMeal) / ps.appetite;
            var externalStimuli = ps.alphah * this.Sh(target);


            var H = math.min(internalUrge + externalStimuli, 1);

            this.te = H / this.hunger;
            this.hunger = H;

            return this.hunger;
        }

        private float L(float t, PhysicalState ps, ISensorableObject target)
        {
            var Ht = H(t, ps, target);
            var internalUrge = ps.Lx(ps.timeSinceLastMating) * (1 - Ht);
            var externalStimuli = this.Sl(target);

            var L = math.min(internalUrge + externalStimuli, 1);

            this.tm = L / this.libido;
            this.libido = L;

            return this.libido;
        }

        private float F(float t, PhysicalState ps, ISensorableObject target)
        {
            var ret = 0;
            var internalUrge = 0;
            //var externalStimuli =

            var F = math.min(1, ret);

            this.tf = F / this.fear;
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
        }
    }
}