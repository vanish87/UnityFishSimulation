using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityTools.Algorithm;

namespace UnityFishSimulation
{
    public class FishSimulatorOffline : Simulator
    {
        public class Problem : IProblem
        {
            public FishModelData BodyModel => this.bodyModel;
            public FishActivationData Activation => this.activationData;
            protected FishModelData bodyModel;
            protected FishActivationData activationData;

            public Problem(FishModelData model, FishActivationData activationData)
            {
                this.bodyModel = model;
                this.activationData = activationData;
            }
        }
        public class Solution : ISolution
        {
            public bool IsDone = false;
            public FishLogger logger = new FishLogger();
        }
        public FishSimulatorOffline(IProblem problem, IDelta dt, IterationAlgorithmMode mode = IterationAlgorithmMode.FullStep) : base(problem, dt, mode)
        {
            this.currentSolution = new Solution();
        }

        public override bool IsSolutionAcceptable(ISolution solution)
        {
            var sol = this.currentSolution as Solution;
            return sol.IsDone;
        }

        public override ISolution Solve(IProblem problem)
        {
            var p = problem as Problem;
            var idt = this.dt as IterationDelta;
            var sol = this.currentSolution as Solution;
            this.ApplyActivation(idt.Current, p.BodyModel, p.Activation);

            var solver = new FishEulerSolver();
            solver.Solve(new FishStructureProblem() { fish = p.BodyModel, dt = idt.DeltaTime });

            // Debug.Log(idt.Current);
            // Debug.Log(idt.DeltaTime);

            sol.IsDone = idt.Current > p.Activation.Interval.y*10;
            sol.logger.Log(p.BodyModel, idt.Current);
            return this.currentSolution;
        }
        protected void ApplyActivation(float t, FishModelData modelData, FishActivationData data)
        {
            var types = new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
            foreach (var type in types)
            {
                var muscle = modelData.GetSpringByType(new List<Spring.Type>() { type });
                var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
                var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);

                {
                    var lvalue = data.Evaluate(t, (type, Spring.Side.Left));
                    var rvalue = data.Evaluate(t, (type, Spring.Side.Right));

                    lvalue = (lvalue + 1) * 0.5f;
                    rvalue = (rvalue + 1) * 0.5f;
                    foreach (var l in muscleLeft)
                    {
                        //l.Activation = act;
                        //l.Activation = cos;// 
                        l.Activation = lvalue;
                    }
                    foreach (var r in muscleRight)
                    {
                        //r.Activation = 1 - act;
                        //r.Activation = 1 - cos;// 
                        r.Activation = rvalue;
                        // r.Activation = 1-lvalue;
                    }
                }
            }
        }
    }
}
