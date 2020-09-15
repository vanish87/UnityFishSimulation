using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;

namespace UnityFishSimulation
{
    public class FishSimulatorRunner : SingletonMonoBehaviour<FishSimulatorRunner>, FishLauncher.ILauncherUser
    {
        [SerializeField] protected FishSimulator simulator;
        [SerializeField] protected FishSimulator.ControllerProblem problem;
        [SerializeField] protected FishSimulator.Delta delta;

        public FishSimulator.ControllerProblem Controller => this.problem;

        public Environment Runtime { get; set; }

        public int Order => (int)FishLauncher.LauncherOrder.LowLevel;

        public void OnLaunchEvent(FishLauncher.Data data, Launcher<FishLauncher.Data>.LaunchEvent levent)
        {
            switch (levent)
            {
                case FishLauncher.LaunchEvent.Init:
                    {
                        this.problem = new FishSimulator.ControllerProblem();
                        this.delta = new FishSimulator.Delta();

                        //this.simulator = new FishSimulator(this.problem, this.delta);
                        this.simulator = this.GetComponent<FishSimulator>();
                        this.simulator.OnInit(this.problem, this.delta);
                        this.simulator.TryToRun();
                    }
                    break;
                case FishLauncher.LaunchEvent.DeInit:
                    {
                       // this.simulator.Dispose();
                    }
                    break;
                default:
                    break;
            }
        }
    }
}