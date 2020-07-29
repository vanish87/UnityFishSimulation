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
            this.fishData = GeometryFunctions.Load();

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
                GeometryFunctions.Save(this.fishData);
            }
            if(Input.GetKeyDown(KeyCode.L))
            {
                this.fishData = GeometryFunctions.Load();
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

        protected void OnDrawGizmos()
        {
            this.fishData.OnGizmos(GeometryFunctions.springColorMap);
        }
        protected void Step(FishModelData fish)
        {
            this.fishSolver.Step(fish);
        }


        protected void StepMartix(FishModelData fish)
        {
            this.fishMatrixSolver.Step(fish);
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
