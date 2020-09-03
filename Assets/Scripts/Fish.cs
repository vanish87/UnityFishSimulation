using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityFishSimulation
{
    public class Fish : MonoBehaviour
    {
        protected FishBrain fishBrain;
        protected FishBody fishBody;
        [SerializeField] protected FishController fishController;

        internal protected FishSimulator sim;
        internal protected FishBrain Brain => this.fishBrain;
        
        protected void Awake()
        {
            this.fishBody = new FishBody();
            this.fishBrain = new FishBrain();
            this.fishController = new FishController(this.fishBody, this.fishBrain);

            var p = new FishSimulator.ControllerProblem();
            this.sim = new FishSimulator(p, new FishSimulator.Delta());
            p.AddController(this.fishController);
        }

        protected void Update()
        {

        }

        protected void OnDrawGizmos()
        {
            this.fishBody?.modelData.OnGizmos(GeometryFunctions.springColorMap);
        }
    }
}
