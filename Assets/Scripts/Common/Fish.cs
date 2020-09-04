using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace UnityFishSimulation
{
    public class Fish : MonoBehaviour
    {
        /*[SerializeField] protected FishBrain fishBrain;
        [SerializeField] protected FishBody fishBody;
        [SerializeField] protected FishController fishController;

        public Environment Runtime { get; set; }

        public int Order => (int)FishLauncher.LauncherOrder.Default;

        public float Size => this.fishBrain.VisionSize;

        public float3 Position => this.fishBody.Position;

        FishManager FishManager.IManagerUser.Manager { get ; set ; }

        public bool IsSensorable(FishManager.ISpaceObject other)
        {
            var distance = math.distance(this.Position, other.Position) * other.Size;
            return distance < this.fishBrain.SensorSize.y;
        }

        public void OnLaunchEvent(FishLauncher.Data data, Launcher<FishLauncher.Data>.LaunchEvent levent)
        {
            switch (levent)
            {
                case FishLauncher.LaunchEvent.Init:
                    {
                        this.fishBody = new FishBody();
                        this.fishBrain = new FishBrain();
                        //this.fishController = new FishController(this.fishBody, this.fishBrain);

                        FishSimulatorRunner.Instance.Controller.AddController(this.fishController);
                    }
                    break;
                default:
                    break;
            }
        }*/

        protected void OnDrawGizmos()
        {
            //this.fishBody?.modelData.OnGizmos(GeometryFunctions.springColorMap);
        }
    }
}
