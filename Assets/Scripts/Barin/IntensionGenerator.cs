using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;

namespace UnityFishSimulation
{
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
        public AvoidIntension(GameObject closestObj)
        {

        }
    }
    public class EscapedIntension : Intension
    {
        public override Type IntensionType => Type.Escape;
        public EscapedIntension(GameObject closestObj)
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
    public class IntensionGenerator : MonoBehaviour
    {
        public float f0 = 0.5f;//[0.1,0.5]
        public float f1 = 0.8f;//f1 > f0
        public float r = 0.25f;//[0,0.5]

        protected LinkedList<Intension> intensionStack = new LinkedList<Intension>();
        public Intension Generate(Perception perception, Habits habits, MentalState mental, PhysicalState ps, float t)
        {
            Intension intension = default;
            var sensorData = perception.GetSensorData();
            var distance = sensorData.closestDistance;
            if (distance > 0)
            {
                var mono = sensorData.closestObject as MonoBehaviour;
                intension = new AvoidIntension(mono.gameObject);
            }
            else
            {
                //most dangerous predator m
                var Fm = distance;// collisions.cloesetObj;
                var F = 0;// mental.F(t, ps, perception.focusser.target);
                if (F > this.f0)
                {
                    if (Fm < this.f1 && habits.schooling)
                    {
                        intension = new SchoolIntension();
                    }
                    else
                    {
                        var mono = sensorData.closestObject as MonoBehaviour;
                        intension = new EscapedIntension(mono.gameObject);
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


            LogTool.AssertIsTrue(intension != null);

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
            var H = 0;// mental.H(t, ps, perception.focusser.target);
            var L = 0;// mental.L(t, ps, perception.focusser.target);

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
}
