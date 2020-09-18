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
        protected Perception perception;

        protected LinkedList<MuscleMC> muscleMCs = new LinkedList<MuscleMC>();
        [SerializeField] protected BalanceMC balanceMC; 

        [SerializeField] protected string currentMC;
        [SerializeField] protected SwimMC swim;
        [SerializeField] protected TurnLeftMC turn;

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

            this.perception = this.GetComponentInChildren<Perception>();
            this.perception.Init();

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
            this.muscleMCs.Clear();

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

        protected void UpdatePerception(FishSimulator.Delta delta)
        {
            this.perception.SensorUpdate(delta);
        }
        protected void UpdateBrain(FishSimulator.Delta delta)
        {
            this.brain.UpdateBrain(delta, this.perception);
        }
        protected void UpdateFocusser(FishSimulator.Delta delta)
        {
            this.perception.FocusserUpdate(this.brain.CurrentIntension, this.brain.CurrentDesire);
        }
        protected void UpdateMotorController(FishSimulator.Delta delta)
        {
            var behaviorRoutine = this.GenerateBehaviorRoutine(this.brain.CurrentIntension, this.perception);

            foreach (var mc in behaviorRoutine.ToMC())
            {
                if(mc is MuscleMC)
                {
                    var mmc = mc as MuscleMC;
                    if(this.muscleMCs.Count > 1)
                    {
                        this.muscleMCs.RemoveLast();
                    }
                    this.muscleMCs.AddLast(mmc);
                }
                if(mc is BalanceMC)
                {
                    this.balanceMC = mc as BalanceMC;
                }
            }

            this.ApplyBehaviorRoutine(delta);
        }
        protected BehaviorRoutine GenerateBehaviorRoutine(Intension intension, Perception perception)
        {
            //MC logical
            var ret = new BehaviorRoutine();
            ret.Init(intension, perception);

            return ret;
        }
        protected void ApplyBehaviorRoutine(FishSimulator.Delta delta)
        {
            var types = new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };

            // foreach (var mc in this.muscleMCs)
            var mc = this.muscleMCs.FirstOrDefault();
            if(mc != null)
            {
                var data = mc.ActivationData;

                foreach (var type in types)
                {
                    //get motor controller from brain
                    var parameter = mc.GetParameter(type);

                    data.ApplyActivation(delta.local, type, this.body.modelData, parameter);
                }
                if(delta.local > data.Interval.y)
                {
                    delta.local = 0;
                    this.muscleMCs.RemoveFirst();
                }
            }

            if(this.balanceMC != null)
            {
                var lfin = this.body.modelData.FishPectoralFins[0];
                var rfin = this.body.modelData.FishPectoralFins[1];

                lfin.Angle = math.lerp(lfin.Angle, math.PI/2 + this.balanceMC.lFin, 0.3f);
                rfin.Angle = math.lerp(rfin.Angle, math.PI/2 + this.balanceMC.rFin, 0.3f);
            }

            //Debug data
            var fmc = this.muscleMCs.FirstOrDefault();
            if(fmc != null)
            {
                if (fmc is SwimMC) this.swim = fmc as SwimMC;
                if (fmc is TurnLeftMC) this.turn = fmc as TurnLeftMC;
                this.currentMC = fmc.ToString();
            }
            else
            {
                this.currentMC = "None";
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

            this.logger.Log(this.body.modelData, delta.current);
        }

        public void MainUpdate(FishSimulator.Delta delta, FishSimulator.Solution solution)
        {
            this.localDelta.current += delta.deltaTime;
            this.localDelta.local += delta.deltaTime;
            this.localDelta.deltaTime = delta.deltaTime;

            this.UpdatePerception(this.localDelta);
            this.UpdateBrain(this.localDelta);
            this.UpdateFocusser(this.localDelta);
            this.UpdateMotorController(this.localDelta);
            this.UpdateBody(this.localDelta);
            this.UpdateSolution(this.localDelta, solution);
        }
    }
}

