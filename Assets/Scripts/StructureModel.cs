using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{
    #if USE_EDITOR_EXC
    //[ExecuteInEditMode]
    #endif

    public class StructureModel : MonoBehaviour
    {
        
        protected static Dictionary<Spring.Type, Color> springColorMap = new Dictionary<Spring.Type, Color>()
        {
            {Spring.Type.Cross , Color.gray },
            {Spring.Type.MuscleFront, Color.red },
            {Spring.Type.MuscleMiddle, Color.green },
            {Spring.Type.MuscleBack, Color.blue },
            {Spring.Type.Normal, Color.cyan },
        };       

        [SerializeField] protected FishModelData fishData = new FishModelData();
        [SerializeField] protected FishEularSolver fishSolver = new FishEularSolver();
        [SerializeField] protected FishMatrixSolver fishMatrixSolver = new FishMatrixSolver();
        [SerializeField] protected List<MassPoint> runtimeList;
        [SerializeField] protected List<Spring> runtimeMuscleList;
        [SerializeField] protected List<Spring> runtimeSpringList;

        protected Graph<MassPoint, Spring> FishGraph { get => this.fishData.FishGraph; }

        protected float3 totalForce;

        public List<Spring> GetSpringByType(List<Spring.Type> types)
        {
            return this.fishData.GetSpringByType(types);
        }

        protected void Start()
        {
            this.Load();

            //GeometryFunctions.InitNewFishModel(this.fishData);
            this.RefreshRuntimeList();
        }        

        protected void RefreshRuntimeList()
        {
            this.runtimeList = this.FishGraph.Nodes.ToList();
            this.runtimeSpringList = this.FishGraph.Edges.ToList();
            this.runtimeMuscleList = this.GetSpringByType(
                new List<Spring.Type>{
                    Spring.Type.MuscleBack,
                    Spring.Type.MuscleMiddle,
                    Spring.Type.MuscleFront }
                );
        }
        

        protected void Update()
        {
            if(Input.GetKeyDown(KeyCode.S))
            {
                this.Save();
            }
            if(Input.GetKeyDown(KeyCode.L))
            {
                this.Load();
                this.RefreshRuntimeList();
            }

            if(Input.GetKey(KeyCode.G))
            {
                this.Step(this.fishData);
            }

            if (Input.GetKey(KeyCode.P))
            {
                foreach (var value in Enumerable.Range(1, 500))
                {
                    this.StepMartix(this.fishData);
                }
            }
        }

        protected void Save()
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, "fish.model");
            FileTool.Write(path, this.fishData);
            LogTool.Log("Saved " + path);
        }
        protected void Load()
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, "fish.model");
            this.fishData = FileTool.Read<FishModelData>(path);
            LogTool.Log("Loaded " + path);
        }

        protected void OnDrawGizmos()
        {
            if(this.FishGraph != null)
            {
                foreach( var edge in this.FishGraph.Edges)
                {
                    using (new GizmosScope(springColorMap[edge.SpringType], Matrix4x4.identity))
                    {
                        edge.OnGizmos();
                    }
                }
            }
            foreach (var n in this.FishGraph.Nodes)
            {
                n.OnGizmos(50 * Unit.WorldMMToUnityUnit);
                //Gizmos.DrawLine(n.Position, n.Position + n.Velocity);
            }

            foreach (var n in this.fishData.FishNormalFace) n.OnGizmos(200 * Unit.WorldMMToUnityUnit);

            //Gizmos.DrawLine(Vector3.zero, this.totalForce);

            using(new GizmosScope(Color.red, Matrix4x4.identity))
            {
                Gizmos.DrawLine(this.fishData.GeometryCenter, this.fishData.GeometryCenter + this.fishData.Direction);
                Gizmos.DrawLine(this.fishData.Head.Position, this.fishData.Head.Position + this.fishData.Velocity);
            }
        }
        protected void Step(FishModelData fish)
        {
            foreach (var value in Enumerable.Range(1, 10))
            {
                this.fishSolver.PreSolve(fish);
                this.fishSolver.ApplyForces(fish);
                this.fishSolver.Intergrate(fish);
                this.fishSolver.PostSolve(fish);
            }
        }


        protected void StepMartix(FishModelData fish)
        {
            this.fishMatrixSolver.PreSolve(fish);
            this.fishMatrixSolver.ApplyForces(fish);
            this.fishMatrixSolver.Intergrate(fish);
            this.fishMatrixSolver.PostSolve(fish);
        }
        

        void Print(Matrix<float3> mat, string name, bool value = false)
        {
            var dim = mat.Size;

            var csv = "";
            for (var i = 0; i < dim.x; ++i)
            {
                for (var j = 0; j < dim.y; ++j)
                {
                    var num = (math.length(mat[i, j]) != 0) ? (value ? mat[i, j].ToString():"1") : " ";
                    //if (i > j) num = " ";
                    csv += num + ",";
                }
                csv += "\n";
            }

            var path = System.IO.Path.Combine(Application.streamingAssetsPath, name+".csv");
            System.IO.File.WriteAllText(path, csv);
        }

        /*protected float3 GetB( i)
        {
            var B = i.
        }*/

    }
}
