using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var distance = sensorData.closestDangerObj==null?-1:sensorData.closestDangerObj.distance;
            if (distance > 0)
            {
                var mono = sensorData.closestDangerObj.obj as MonoBehaviour;
                intension = new AvoidIntension(mono.gameObject);
            }
            else
            {
                //fear of most dangerous predator m
                var p = sensorData.GetClosestByType(ObjectType.Predator);
                var Fm = ps.Fi(p.distance);
                var F = mental.fear;
                if (F > this.f0)
                {
                    if (Fm < this.f1 && habits.schooling)
                    {
                        intension = new SchoolIntension();
                    }
                    else
                    {
                        var mono = p.obj as MonoBehaviour;
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
            var H = mental.hunger;
            var L = mental.libido;

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
