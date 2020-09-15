using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;

namespace UnityFishSimulation
{
    public class FishController : MonoBehaviour, FishLauncher.ILauncherUser
    {
        public enum SolverType
        {
            Euler,
            Matrix,
        }
        public enum ControlMode
        {
            Learning,
            Normal,
        }

        public Environment Runtime { get; set; }
        public int Order => (int)FishLauncher.LauncherOrder.Default;
        public void OnLaunchEvent(FishLauncher.Data data, Launcher<FishLauncher.Data>.LaunchEvent levent)
        {
            switch (levent)
            {
                case FishLauncher.LaunchEvent.Init:
                    {
                        this.Init();
                    }
                    break;
                default:
                    break;
            }
        }

        protected FishBody body;
        protected FishBrain brain;

        protected SolverType solverType = SolverType.Euler;
        protected IAlgorithm solver;

        protected FishSimulator.Delta localDelta;
        protected FishLogger logger;

        [SerializeField] protected FishActivationData customData;
        [SerializeField] public List<MassPoint> runtimeList;
        [SerializeField] public List<Spring> runtimeMuscleList;
        [SerializeField] public List<Spring> runtimeSpringList;
        [SerializeField] public List<FinFace> runtimeFinList;

        // public FishBrain Brain => this.brain;

        // public bool IsDone { get; set; }

        public void Init()
        {
            this.body = this.GetComponentInChildren<FishBody>();
            this.body.Init();

            this.brain = this.GetComponentInChildren<FishBrain>();
            this.brain.Init();

            this.logger = this.logger ?? new FishLogger();
            this.localDelta = this.localDelta ?? new FishSimulator.Delta();

            this.Reset();
            FishSimulatorRunner.Instance.Controller.AddController(this);

        }
        public void Reset()
        {
            //TODO move load to Body
            this.body.modelData = GeometryFunctions.Load();

            if (this.solverType == SolverType.Euler)
            {
                this.solver = new FishEulerSolver();
            }
            else
            {
                this.solver = new FishMatrixSolver();
            }
            this.localDelta.Reset();

            this.runtimeList = this.body.modelData.FishGraph.Nodes.ToList();
            this.runtimeSpringList = this.body.modelData.FishGraph.Edges.ToList();
            this.runtimeMuscleList = this.body.modelData.GetSpringByType(
                                                new List<Spring.Type>{
                                                    Spring.Type.MuscleBack,
                                                    Spring.Type.MuscleMiddle,
                                                    Spring.Type.MuscleFront }
                                                );

            this.runtimeFinList = this.body.modelData.FishPectoralFins;
        }

        protected void UpdateBrain(FishSimulator.Delta delta)
        {
            this.brain.UpdateBrain(delta.deltaTime);
        }

        protected void ApplyBehaviorRoutine(FishSimulator.Delta delta)
        {
            var t = delta.current;
            var types = new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };


            var routine = this.brain.GetBehaviorRoutine();

            foreach (var mc in routine.ToMC().Select(mc => mc as MuscleMC))
            {
                var data = mc.ActivationData;

                foreach (var type in types)
                {
                    //get motor controller from brain
                    var parameter = mc.GetParameter(type);

                    this.ApplyActivation(t, mc.ActivationData, type, parameter);
                }
            }

        }

        protected void ApplyActivation(float t, FishActivationData data, Spring.Type type, MuscleMC.Parameter muscleMC)
        {

            var muscle = body.modelData.GetSpringByType(new List<Spring.Type>() { type });
            var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
            var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);

            var f = muscleMC.frequency;
            var a = muscleMC.amplitude;
            {
                var lvalue = data.Evaluate(t * f, (type, Spring.Side.Left)) * a;
                var rvalue = data.Evaluate(t * f, (type, Spring.Side.Right)) * a;

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
                }
            }


        }
        protected void UpdateBody(FishSimulator.Delta delta)
        {
            this.solver.Solve(new FishStructureProblem() { fish = body.modelData, dt = delta.deltaTime });
        }

        protected void UpdateSolution(FishSimulator.Delta delta, FishSimulator.Solution solution)
        {
            // if (this.controlMode == ControlMode.Normal)
            // {

            // }
            // else
            // {
            //     var data = this.customData.Interval;
            //     this.IsDone = delta.current > data.y;
            // }
            /*var data = this.brain.Current;
            this.IsDone = delta.current > data.Interval.y;
*/

            // if (this.IsDone) delta.Reset();

            /*return;
            if (delta.current > this.trajactory.GetValueX(this.currentIndex))
            {
                this.trajactory[this.currentIndex] = this.body.modelData.Head.Position;
                this.velocity[this.currentIndex] = this.body.modelData.Velocity;
                this.currentIndex++;
            }*/

            this.logger.Log();
        }

        public void MainUpdate(FishSimulator.Delta delta, FishSimulator.Solution solution)
        {
            this.localDelta.current += delta.deltaTime;
            this.localDelta.deltaTime = delta.deltaTime;

            this.UpdateBrain(this.localDelta);
            this.ApplyBehaviorRoutine(this.localDelta);
            this.UpdateBody(this.localDelta);
            this.UpdateSolution(this.localDelta, solution);
        }
    }
}

