using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishMC : MonoBehaviour
    {
        [SerializeField] protected FishSwimmingMC fishMC;

        protected FishSimulator sim;

        protected void Start()
        {
            fishMC = new FishSwimmingMC();
            sim = new FishSimulator(FishSimulator.SolverType.Euler, this.fishMC, new FishSimulator.Delta());
            sim.End((p, s, d, a) => sim.TryToRun());

            sim.TryToRun();
        }

        protected void OnDrawGizmos()
        {
            this.sim?.OnGizmos();
        }
    }
}
