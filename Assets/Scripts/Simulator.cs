using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;

namespace UnityFishSimulation
{
    public interface IRunable<Output>
    {
        void Start();
        void End();
        void Step(float dt);
        Output GetOutput();
    }

    [System.Serializable]
    public class Simulator<Output, Runner> : ObjectStateMachine<Simulator<Output, Runner>> where Runner : IRunable<Output>
    {
        [System.Serializable]
        public class SimulatorSateReady : StateBase<Simulator<Output, Runner>>
        {
            public static SimulatorSateReady Instance { get => instance; }
            protected static SimulatorSateReady instance = new SimulatorSateReady();

            internal override void Enter(Simulator<Output, Runner> obj)
            {
            }
            internal override void Excute(Simulator<Output, Runner> obj)
            {
            }

            internal override void Leave(Simulator<Output, Runner> obj)
            {

            }

        }

        [System.Serializable]
        public class SimulatorSateRunning : StateBase<Simulator<Output, Runner>>
        {
            public static SimulatorSateRunning Instance { get => instance; }
            protected static SimulatorSateRunning instance = new SimulatorSateRunning();

            internal override void Enter(Simulator<Output, Runner> obj)
            {
                obj.runner?.Start();
            }
            internal override void Excute(Simulator<Output, Runner> obj)
            {
                obj.runner?.Step(Solver.dt);
            }

            internal override void Leave(Simulator<Output, Runner> obj)
            {
                obj.runner?.End();
            }
        }

        public class SimulatorSateRunningTimer : SimulatorSateRunning
        {
            protected float current = 0;
            internal override void Enter(Simulator<Output, Runner> obj)
            {
                base.Enter(obj);
                this.current = 0;
            }
        }

        [System.Serializable]
        public class SimulatorSateDone : StateBase<Simulator<Output, Runner>>
        {
            public static SimulatorSateDone Instance { get => instance; }
            protected static SimulatorSateDone instance = new SimulatorSateDone();

            internal override void Enter(Simulator<Output, Runner> obj)
            {
            }
            internal override void Excute(Simulator<Output, Runner> obj)
            {
            }

            internal override void Leave(Simulator<Output, Runner> obj)
            {

            }
        }

        protected internal Runner runner;

        public virtual SimulatorSateReady Ready { get => SimulatorSateReady.Instance; }
        public virtual SimulatorSateRunning Running { get => SimulatorSateRunning.Instance; }
        public virtual SimulatorSateDone Done { get => SimulatorSateDone.Instance; }

        protected void Start()
        {
            this.Reset();
        }

        public void Reset()
        {
            this.ChangeState(this.Ready);
        }

        /*

        public Output SimulateStep(float dt = Solver.dt)
        {
            if (this.runner == null) return default;

            if(this.currentState_ != this.Running)
            {
                this.ChangeState(this.Running);
            }

            this.runner.Step(dt);

            this.ChangeState(this.Done);

            return this.runner.GetOutput();
        }

        public Output Simulate(float from, float to, float dt = Solver.dt)
        {
            if (this.runner == null) return default;

            if (this.currentState_ != this.Running)
            {
                this.ChangeState(this.Running);
            }

            var current = from;
            while (current < to)
            {
                this.runner.Step(dt);
                current += dt;
            }

            this.ChangeState(this.Done);

            return this.runner.GetOutput();
        }*/
    }
}