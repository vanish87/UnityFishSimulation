using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishTurn : MonoBehaviour
    {
        [SerializeField] protected FishTurnMC fishMC;

        [SerializeField, Range(0, 3.14f)] protected float angle = math.PI / 2;
        [SerializeField] protected FishSimulator sim;

        protected void Start()
        {
            fishMC = new FishTurnMC();
            sim = new FishSimulator(FishSimulator.SolverType.Euler, this.fishMC, new FishSimulator.Delta());
            sim.End((p, s, d, a) => sim.TryToRun());

            sim.ResetAndRun();
        }

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                FishActivationData.Save(this.fishMC.Current);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                sim.ResetAndRun();
            }
            
        }

        protected void OnDrawGizmos()
        {
            this.sim?.OnGizmos();
        }
    }
}