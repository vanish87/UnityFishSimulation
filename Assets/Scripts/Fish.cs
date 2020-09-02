using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityFishSimulation
{
    public class Fish : MonoBehaviour
    {
        protected FishBrain fishBrain;
        protected FishSimulator fishSimulator;

        protected void Start()
        {
            
        }

        protected void Update()
        {
            this.fishBrain.Update(Time.deltaTime);
        }
    }
}
