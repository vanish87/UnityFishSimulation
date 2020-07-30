using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishSimulator : Simulator<FishSimulator.Output, FishSimulator.Runner>
    {
        public enum RunMode
        {
            PerStep,
            FullInterval,
        }
        public enum SolverType
        {
            Eular,
            Matrix,
        }
        public class Runner : IRunable<Output>
        {
            protected DiscreteFunction<float, float3> trajactory;
            protected FishModelData fish;
            protected FishSolver solver;

            public Runner(FishModelData fish, FishSolver solver)
            {
                this.fish = fish;
                this.solver = solver;
                this.trajactory = new DiscreteFunction<float, float3>();
            }

            public void Start()
            {
                this.trajactory.ResetValues();
            }
            public void End()
            {
            }

            public void Step(float dt)
            {
                this.solver.Step(this.fish, dt);
                this.UpdateOutput();
            }

            public Output GetOutput()
            {
                return new Output() { trajactory = this.trajactory };
            }


            protected void UpdateOutput()
            {
                this.trajactory.SetValueY(0, 1);
            }
        }

        public class Output
        {
            public DiscreteFunction<float, float3> trajactory;
        }


        public class FishSimulatorRunningState : SimulatorSateRunningTimer
        {
            internal override void Enter(Simulator<Output, Runner> obj)
            {
                base.Enter(obj);

                var fishSim = obj as FishSimulator;
                fishSim.ResetData();
            }
            internal override void Excute(Simulator<Output, Runner> obj)
            {
                var fishSim = obj as FishSimulator;
                var dt = Solver.dt;
                obj.runner?.Step(dt);
                current += dt;

                if (current > fishSim.to) fishSim.ChangeState(SimulatorSateDone.Instance);
            }
        }

        [SerializeField] protected RunMode runMode = RunMode.PerStep;
        [SerializeField] protected SolverType solverType = SolverType.Eular;

        [SerializeField] protected FishModelData fish;
        [SerializeField] protected FishSolver solver;
        [SerializeField] internal protected float from = 0;
        [SerializeField] internal protected float to = 0;

        public FishModelData Fish { get => this.fish??GeometryFunctions.Load(); }

        public void StartSimulation()
        {
            this.ChangeState(this.Running);
        }

        public void SetStartEnd(float from, float to) 
        {
            this.from = from;
            this.to = to;
        }

        public override SimulatorSateRunning Running { get => FishSimulatorRunningState.Instance; }

        protected internal void ResetData()
        {
            this.fish = GeometryFunctions.Load().DeepCopy();
            if (this.solverType == SolverType.Eular)
            {
                this.solver = new FishEularSolver();
            }
            else
            {
                this.solver = new FishMatrixSolver();
            }
            this.runner = new Runner(this.fish, this.solver);
        }
    }
}