using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;

namespace UnityFishSimulation
{
    public enum ObjectType
    {
        Predator,
        Prey,
        Obstacle,
        Food,
        Mate,
    }
    public interface ISensorableObject
    {
        float3 Position { get; }
        float Size { get; }
        ObjectType ObjType { get; }
        float GetDistance(ISensorableObject other);
    }
    public interface IDebug
    {
        bool DrawGizmo { get; set; }
    }
    public class FishManager : MonoBehaviour, FishLauncher.ILauncherUser
    {
        public interface IManagerUser
        {
            FishManager Manager { get; set; }
        }

        public Environment Runtime { get; set; }

        public int Order => (int)FishLauncher.LauncherOrder.LowLevel;

        [SerializeField] protected GameObject fishPrefab;

        public List<ISensorableObject> SensorableObjects => this.sensorableObjectsList;
        [SerializeField] protected List<ISensorableObject> sensorableObjectsList = new List<ISensorableObject>();

        public void OnLaunchEvent(FishLauncher.Data data, Launcher<FishLauncher.Data>.LaunchEvent levent)
        {
            switch (levent)
            {
                case FishLauncher.LaunchEvent.Init:
                    {
                        this.sensorableObjectsList.Clear();
                        this.sensorableObjectsList.AddRange(ObjectTool.FindAllObject<ISensorableObject>());
                        this.SetupUser();
                    }
                    break;
                default:
                    break;
            }
        }

        protected void SetupUser()
        {
            foreach (var user in this.GetComponentsInChildren<IManagerUser>()) user.Manager = this;
        }
    }
}