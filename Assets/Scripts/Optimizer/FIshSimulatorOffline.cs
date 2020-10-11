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


            public FishBody Body=>this.fishBody;
            public MuscleMC MC=>this.mcData;
            protected FishBody fishBody;
            protected MuscleMC mcData;

            public Problem(FishModelData model, FishActivationData activationData)
            {
                this.UpdateData(model, activationData);
            }
            public Problem(FishBody fish, MuscleMC mc)
            {
                this.fishBody = fish;
                this.mcData = mc;
            }
            public void UpdateData(FishModelData modelData, FishActivationData activationData)
            {
                this.bodyModel = modelData;
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

        public void Restart()
        {
            this.currentSolution = new Solution();
            this.Reset();
            this.TryToRun();
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
            var model = p.BodyModel;
            var activation = p.Activation;
            if(p.BodyModel != null)
            {
                this.ApplyActivation(idt.Current, p.BodyModel, p.Activation);
            }
            else
            {
                this.ApplyActivation(idt.Current, p.Body, p.MC);
                model = p.Body.modelData;
                activation = p.MC.ActivationData;
            }

            var solver = new FishEulerSolver();
            solver.Solve(new FishStructureProblem() { fish = model, dt = idt.DeltaTime });

            // Debug.Log(idt.Current);
            // Debug.Log(idt.DeltaTime);

            sol.IsDone = idt.Current > activation.Interval.y;
            sol.logger.Log(model, idt.Current);
            return this.currentSolution;
        }
        protected void ApplyActivation(float t, FishBody body, MuscleMC mc)
        {
            var types = new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
            foreach (var type in types)
            {
                mc.ActivationData.ApplyActivation(t, type, body.modelData, mc.GetParameter(type));
            }
 
        }
        protected void ApplyActivation(float t, FishModelData modelData, FishActivationData data)
        {
            var types = new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };
            foreach (var type in types)
            {
                data.ApplyActivation(t, type, modelData);
            }
        }
    }
}
