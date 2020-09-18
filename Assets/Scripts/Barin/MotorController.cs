using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Math;

namespace UnityFishSimulation
{
    [System.Serializable]
    public class MotorController
    {

    }

    [System.Serializable]
    public abstract class MuscleMC : MotorController
    {
        [System.Serializable]
        public class Parameter
        {
            public readonly float2 aMinMax = new float2(0, 1);
            public readonly float2 fMinMax = new float2(0, 0.075f);
            public float amplitude = 1;
            public float frequency = 1;
        }
        protected FishActivationData activationData;
        protected Dictionary<Spring.Type, Parameter> muscleControlParamters;
        [SerializeField] protected List<Parameter> parameters;//Debug data
        protected abstract List<Spring.Type> GetSpringTypes();
        protected abstract string FileName { get; }

        public FishActivationData ActivationData => this.activationData;
        public abstract Parameter GetParameter(Spring.Type type);

        public MuscleMC()
        {
            this.muscleControlParamters = new Dictionary<Spring.Type, Parameter>();
            var types = this.GetSpringTypes();
            foreach (var t in types)
            {
                this.muscleControlParamters.Add(t, new Parameter());
            }

            this.activationData = FishActivationData.Load(this.FileName);

            this.parameters = this.muscleControlParamters.Values.ToList();
        }
    }

    [System.Serializable]
    public class SwimMC : MuscleMC
    {
        [SerializeField, Range(0, 1)] protected float speed = 1;

        protected DiscreteFunction<float, float2> speedParameterMap;
        protected override string FileName => "Swimming";
        protected override List<Spring.Type> GetSpringTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
        }
        public override Parameter GetParameter(Spring.Type type)
        {
            if (this.muscleControlParamters.ContainsKey(type))
            {
                var parameter = this.muscleControlParamters[type];
                var mapped = this.speedParameterMap.Evaluate(this.speed);
                parameter.amplitude = math.lerp(parameter.amplitude, mapped.x, 0.3f);
                parameter.frequency = math.lerp(parameter.frequency, mapped.y, 0.3f);
                return parameter;
            }
            //convert from speed to parameter
            return new Parameter();
        }

        public SwimMC() : base()
        {
            var parameterVec = new Vector<float2>(4);
            parameterVec[0] = new float2(0, 0);
            parameterVec[1] = new float2(0.6f, 1.2f);
            parameterVec[2] = new float2(1.0f, 1.5f);
            parameterVec[3] = new float2(1.2f, 2f);
            this.speedParameterMap = new DiscreteFunction<float, float2>(0, 1, parameterVec);
        }
        public void UpdateSpeed(float distance)
        {
            var maxDis = 20f;
            this.speed = math.saturate(distance / maxDis);
        }

    }

    [System.Serializable]
    public class TurnLeftMC : MuscleMC
    {
        [SerializeField, Range(0, math.PI / 2)] protected float angle = 0f;
        protected DiscreteFunction<float, float2> angleParameterMap;
        protected override string FileName => "TurnLeft";
        protected override List<Spring.Type> GetSpringTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle };
        }

        public override Parameter GetParameter(Spring.Type type)
        {
            if (this.muscleControlParamters.ContainsKey(type))
            {
                var parameter = this.muscleControlParamters[type];
                var mapped = this.angleParameterMap.Evaluate(this.angle);
                parameter.amplitude = math.lerp(parameter.amplitude, mapped.x, 0.3f);
                parameter.frequency = math.lerp(parameter.frequency, mapped.y, 0.3f);
                return parameter;
            }
            //convert from speed to parameter
            return new Parameter();
        }
        public TurnLeftMC() : base()
        {
            var parameterVec = new Vector<float2>(4);
            parameterVec[3] = new float2(0, 0);
            parameterVec[2] = new float2(0.2f, 0.2f);
            parameterVec[1] = new float2(0.5f, 0.5f);
            parameterVec[0] = new float2(1.0f, 1.0f);
            this.angleParameterMap = new DiscreteFunction<float, float2>(0, math.PI / 2, parameterVec);
        }
        public void UpdateAngle(float angle)
        {
            this.angle = math.acos(angle);
        }
    }
    [System.Serializable]
    public class TurnRightMC : MuscleMC
    {
        [SerializeField, Range(math.PI / 2, math.PI)] protected float angle = math.PI;
        protected DiscreteFunction<float, float2> angleParameterMap;
        protected override string FileName => "TurnRight";
        protected override List<Spring.Type> GetSpringTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle };
        }

        public override Parameter GetParameter(Spring.Type type)
        {
            if (this.muscleControlParamters.ContainsKey(type))
            {
                var parameter = this.muscleControlParamters[type];
                var mapped = this.angleParameterMap.Evaluate(this.angle);
                parameter.amplitude = math.lerp(parameter.amplitude, mapped.x, 0.3f);
                parameter.frequency = math.lerp(parameter.frequency, mapped.y, 0.3f);
                return parameter;
            }
            //convert from speed to parameter
            return new Parameter();
        }
        public TurnRightMC() : base()
        {
            var parameterVec = new Vector<float2>(4);
            parameterVec[0] = new float2(0, 0);
            parameterVec[1] = new float2(0.2f, 0.2f);
            parameterVec[2] = new float2(0.5f, 0.5f);
            parameterVec[3] = new float2(1.0f, 1.0f);
            this.angleParameterMap = new DiscreteFunction<float, float2>(math.PI / 2, math.PI, parameterVec);

        }
        public void UpdateAngle(float angle)
        {
            this.angle = math.acos(angle);
        }
    }

    public class GlideMC : MuscleMC
    {
        protected float time = 0;
        protected override string FileName => "Glide";
        protected override List<Spring.Type> GetSpringTypes()
        {
            return new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
        }

        public override Parameter GetParameter(Spring.Type type)
        {
            //convert from speed to parameter
            return default;
        }
    }

    [System.Serializable]
    public class BalanceMC : MotorController
    {
        public float lFin => this.leftFin;
        public float rFin => this.rightFin;
        protected float3 worldUp = new float3(0, 1, 0);
        [SerializeField] protected float leftFin;
        [SerializeField] protected float rightFin;

        public void UpdateBalance(float3 left)
        {
            left = math.normalize(left);
            var angleWithWorld = math.acos(math.dot(left, this.worldUp));

            this.leftFin = math.PI/2 - angleWithWorld;
            this.rightFin = -leftFin;
        }
    }
}
