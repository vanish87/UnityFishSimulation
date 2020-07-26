using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityTools.Common;

namespace UnityFishSimulation
{
    #if USE_EDITOR_EXC
    //[ExecuteInEditMode]
    #endif
    public class StructureModel : MonoBehaviour
    {
        [System.Serializable]
        public class MassPoint : Point, INode
        {
            public int id;
            public float mass;

            //runtime
            public float3 force;
            public float3 velocity;

            public int Index { get => this.id; set => this.id = value; }
        }

        [System.Serializable]
        public class Spring: Segment<MassPoint>
        {
            public float c = 38;  // elasticity constant
            public float k = 0.1f;// viscosity constant
            public float l = 1;   // rest length
            public override string ToString()
            {
                return "1";
            }
        }

        protected Graph<MassPoint, Spring> fishGraph = new Graph<MassPoint, Spring>(23);
        [SerializeField] protected List<MassPoint> runtimeList;
        [SerializeField] protected List<Spring> runtimeSpringList;

        protected void Start()
        {
            this.Load();
            this.fishGraph.Print();

            this.runtimeList = this.fishGraph.Nodes.ToList();

            //this.fishGraph.AdjMatrix.Clean();
            //this.InitSprings();

            this.runtimeSpringList = new List<Spring>();
            for (var r = 0; r < this.fishGraph.AdjMatrix.Size.x; ++r)
            {
                for (var c = 0; c < this.fishGraph.AdjMatrix.Size.y; ++c)
                {
                    var edge = this.fishGraph.GetEdge(r, c);
                    if (edge == null || r == c) continue;
                    if (this.runtimeSpringList.Contains(edge)) continue;

                    this.runtimeSpringList.Add(edge);
                }
            }
        }

        protected void InitSprings()
        {
            this.AddSpring(0, 1, 30);
            this.AddSpring(0, 2, 30);
            this.AddSpring(0, 3, 30);
            this.AddSpring(0, 4, 30);

            this.AddSpring(1, 2, 30);
            this.AddSpring(1, 3, 38);
            this.AddSpring(1, 4, 30);
            this.AddSpring(1, 5, 28);//
            this.AddSpring(1, 6, 38);
            this.AddSpring(1, 8, 38);

            this.AddSpring(2, 3, 30);
            this.AddSpring(2, 4, 38);
            this.AddSpring(2, 5, 38);
            this.AddSpring(2, 6, 28);//
            this.AddSpring(2, 7, 38);

            this.AddSpring(3, 4, 30);
            this.AddSpring(3, 6, 38);
            this.AddSpring(3, 7, 28);//
            this.AddSpring(3, 8, 38);

            this.AddSpring(4, 5, 38);
            this.AddSpring(4, 7, 38);
            this.AddSpring(4, 8, 28);//
            //---------------------------------

            this.AddSpring(5, 6, 30);
            this.AddSpring(5, 7, 38);
            this.AddSpring(5, 8, 30);
            this.AddSpring(5, 9, 28);
            this.AddSpring(5, 10, 38);
            this.AddSpring(5, 12, 38);

            this.AddSpring(6, 7, 30);
            this.AddSpring(6, 8, 38);
            this.AddSpring(6, 9, 38);
            this.AddSpring(6, 10, 28);
            this.AddSpring(6, 11, 38);

            this.AddSpring(7, 8, 30);
            this.AddSpring(7, 10, 38);
            this.AddSpring(7, 11, 28);
            this.AddSpring(7, 12, 38);

            this.AddSpring(8, 9, 38);
            this.AddSpring(8, 11, 38);
            this.AddSpring(8, 12, 28);
            //---------------------------------

            this.AddSpring(9, 10, 30);
            this.AddSpring(9, 11, 38);
            this.AddSpring(9, 12, 30);
            this.AddSpring(9, 13, 28);
            this.AddSpring(9, 14, 38);
            this.AddSpring(9, 16, 38);

            this.AddSpring(10, 11, 30);
            this.AddSpring(10, 12, 38);
            this.AddSpring(10, 13, 38);
            this.AddSpring(10, 14, 28);
            this.AddSpring(10, 15, 38);

            this.AddSpring(11, 12, 30);
            this.AddSpring(11, 14, 38);
            this.AddSpring(11, 15, 28);
            this.AddSpring(11, 16, 38);

            this.AddSpring(12, 13, 38);
            this.AddSpring(12, 15, 38);
            this.AddSpring(12, 16, 28);
            //--------------------------------

            this.AddSpring(13, 14, 30);
            this.AddSpring(13, 15, 38);
            this.AddSpring(13, 16, 30);
            this.AddSpring(13, 17, 30);
            this.AddSpring(13, 18, 38);
            this.AddSpring(13, 20, 38);

            this.AddSpring(14, 15, 30);
            this.AddSpring(14, 16, 38);
            this.AddSpring(14, 17, 38);
            this.AddSpring(14, 18, 30);
            this.AddSpring(14, 19, 38);

            this.AddSpring(15, 16, 30);
            this.AddSpring(15, 18, 38);
            this.AddSpring(15, 19, 30);
            this.AddSpring(15, 20, 38);

            this.AddSpring(16, 17, 38);
            this.AddSpring(16, 19, 38);
            this.AddSpring(16, 20, 30);
            //---------------------------------

            this.AddSpring(17, 18, 30);
            this.AddSpring(17, 19, 38);
            this.AddSpring(17, 20, 30);
            this.AddSpring(17, 21, 30);
            this.AddSpring(17, 22, 38);

            this.AddSpring(18, 19, 30);
            this.AddSpring(18, 20, 38);
            this.AddSpring(18, 21, 30);
            this.AddSpring(18, 22, 38);

            this.AddSpring(19, 20, 30);
            this.AddSpring(19, 21, 38);
            this.AddSpring(19, 22, 30);

            this.AddSpring(20, 21, 38);
            this.AddSpring(20, 22, 30);

            this.AddSpring(21, 22, 30);

        }
        protected void AddSpring(int from, int to, float c = 30f, float k = 0.1f)
        {
            var nodes = this.fishGraph.Nodes.ToList();
            var s = new Spring();
            s.Left = nodes[from];
            s.Right = nodes[to];

            s.c = c;
            s.k = k;
            s.l = math.length(nodes[from].Position-nodes[to].Position);

            this.fishGraph.AddEdge(from, to, s);
        }

        protected void Update()
        {
            if(Input.GetKeyDown(KeyCode.S))
            {
                var path = System.IO.Path.Combine(Application.streamingAssetsPath, "fish.graph");
                FileTool.Write(path, this.fishGraph);
            }
            if(Input.GetKeyDown(KeyCode.L))
            {
                this.Load();
                //this.runtimeList = this.fishGraph.Nodes.ToList();
            }

            if(Input.GetKey(KeyCode.G))
            {
                this.Step();
            }
        }

        protected void Load()
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, "fish.graph");
            this.fishGraph = FileTool.Read<Graph<MassPoint, Spring>>(path);
        }

        protected void OnDrawGizmos()
        {
            if(this.fishGraph != null)
            {
                for (var r = 0; r < this.fishGraph.AdjMatrix.Size.x; ++r)
                {
                    for (var c = 0; c < this.fishGraph.AdjMatrix.Size.y; ++c)
                    {
                        var edge = this.fishGraph.GetEdge(r, c);
                        if (edge == null) continue;

                        if (edge.c == 28) Gizmos.color = Color.red;
                        else if (edge.c == 38) Gizmos.color = Color.blue;
                        else Gizmos.color = Color.cyan;
                        
                        edge.OnGizmos();
                    }
                }
            }
            foreach (var n in this.fishGraph.Nodes) n.OnGizmos(50 * Unit.WorldMMToUnityUnit);
        }

        protected void Step()
        {
            var dt = 0.005f;
            foreach(var n in this.fishGraph.Nodes)
            {
                var force = this.GetSpringForce(n);
                n.force = force;

                n.velocity += n.force * dt;
                n.Position += n.velocity * dt;
            }
        }
        protected float3 GetSpringForce(MassPoint i)
        {
            var neighbors = this.fishGraph.GetNeighborsNode(i);
            var ret = float3.zero;

            foreach(var j in neighbors)
            {
                var r = j.Position - i.Position;
                var r_ij = math.length(r);
                var s_ij = this.fishGraph.GetEdge(i, j);
                Assert.IsNotNull(s_ij);

                var e_ij = r_ij - s_ij.l;

                var force_ij = s_ij.c * e_ij * math.normalize(r);
                ret += force_ij;
            }

            return ret;
        }

    }
}
