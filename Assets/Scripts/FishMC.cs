using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityFishSimulation
{
    public class FishMC : MonoBehaviour
    {
        public enum Type
        {
            Swimming,
            TurnRight
        }
        [SerializeField] Type type = Type.Swimming;
        Fish fish;

        public List<AnimationCurve> curves;
        protected void Start()
        {
            this.fish = this.GetComponent<Fish>();

            this.fish.Brain.temp = FishActivationData.Load(type.ToString());
            this.fish.sim.TryToRun();


            this.curves = this.fish.Brain.temp.ToAnimationCurves();
        }
        /*[SerializeField] protected FishController fishMC;

        [SerializeField, Range(0, 3.14f)] protected float Langle = math.PI / 2;
        [SerializeField, Range(0, 3.14f)] protected float Rangle = math.PI / 2;
        [SerializeField] protected FishSimulator sim;

        protected void Start()
        {
            fishMC = new FishController();
            / *sim = new FishSimulator(FishSimulator.SolverType.Euler, this.fishMC, new FishSimulator.Delta());
            sim.End((p, s, d, a) => sim.TryToRun());

            sim.ResetAndRun();* /
        }

        protected void Update()
        {
            if(Input.GetKeyDown(KeyCode.S))
            {
                FishActivationData.Save(this.fishMC.Current);
            }
            / *if (Input.GetKeyDown(KeyCode.R))
            {
                this.fishMC.ReloadData();
            }

            this.sim.runtimeFinList[0].Anlge = Langle;
            this.sim.runtimeFinList[1].Anlge = Langle;* /
        }

        protected void OnDrawGizmos()
        {
        }*/
    }
}
