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
                        this.body = this.GetComponentInChildren<FishBody>();
                        this.body.Init();

                        this.brain = this.GetComponentInChildren<FishBrain>();
                        this.brain.Init();

                        this.logger = this.logger ?? new FishLogger();

                        this.Reset();
                        FishSimulatorRunner.Instance.Controller.AddController(this);
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

        protected FishLogger logger;

        [SerializeField] public List<MassPoint> runtimeList;
        [SerializeField] public List<Spring> runtimeMuscleList;
        [SerializeField] public List<Spring> runtimeSpringList;
        [SerializeField] public List<FinFace> runtimeFinList;

        // public FishBrain Brain => this.brain;

        // public bool IsDone { get; set; }

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
            var routine = this.brain.GetBehaviorRoutine();

            foreach (var mc in routine.ToMC().Select(mc => mc as MuscleMC))
            {
                var data = mc.ActivationData;

                //get motor controller from brain
                var types = new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };

                foreach (var type in types)
                {
                    var parameter = mc.GetParameter(type);

                    var t = delta.current;
                    var muscle = body.modelData.GetSpringByType(new List<Spring.Type>() { type });
                    var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
                    var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);


                    if (data.HasType(type))
                    {
                        var tuning = data.Tuning.GetDataByType(type);
                        var value = data.Evaluate(t * tuning.frequency * parameter.frequency, type, data.Tuning.useFFT);
                        //value = data.Evaluate(t * tuning.frequency, type, false);
                        var lvalue = this.ApplyTuning(tuning, value);
                        var rvalue = this.ApplyTuning(tuning, 1 - value);

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
            }
        }

        protected void UpdateBody(FishSimulator.Delta delta)
        {
            this.solver.Solve(new FishStructureProblem() { fish = body.modelData, dt = delta.deltaTime });
        }

        protected void UpdateSolution(FishSimulator.Delta delta, FishSimulator.Solution solution)
        {
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
        protected float ApplyTuning(TuningData.SpringToData data, float value)
        {
            value = math.clamp((value - 0.5f) * 2 * data.amplitude, -1, 1);
            value = (value + 1) * 0.5f;
            value += data.offset;
            value = math.saturate(value);
            return value;
        }

        public void MainUpdate(FishSimulator.Delta delta, FishSimulator.Solution solution)
        {
            this.UpdateBrain(delta);
            this.ApplyBehaviorRoutine(delta);
            this.UpdateBody(delta);
            this.UpdateSolution(delta, solution);
        }
    }
}

