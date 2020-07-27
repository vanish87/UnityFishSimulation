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
            public enum Type
            {
                Cross,
                Muscle,
                Normal,
            }

            protected Dictionary<Type, float> elasticMap = new Dictionary<Type, float>()
            {
                {Type.Cross , 38f },
                {Type.Muscle, 28f },
                {Type.Normal, 30f },
            };

            public float c = 38;  // elasticity constant
            public float k = 0.1f;// viscosity constant
            public float l = 1;   // rest length
            public Type type = Type.Normal;


            public Spring(Type type)
            {
                this.type = type;
                this.c = elasticMap[this.type];
            }

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
            this.AddSpring(0, 1, Spring.Type.Normal);
            this.AddSpring(0, 2, Spring.Type.Normal);
            this.AddSpring(0, 3, Spring.Type.Normal);
            this.AddSpring(0, 4, Spring.Type.Normal);

            this.AddSpring(1, 2, Spring.Type.Normal);
            this.AddSpring(1, 3, Spring.Type.Cross);
            this.AddSpring(1, 4, Spring.Type.Normal);
            this.AddSpring(1, 5, Spring.Type.Muscle);//
            this.AddSpring(1, 6, Spring.Type.Cross);
            this.AddSpring(1, 8, Spring.Type.Cross);

            this.AddSpring(2, 3, Spring.Type.Normal);
            this.AddSpring(2, 4, Spring.Type.Cross);
            this.AddSpring(2, 5, Spring.Type.Cross);
            this.AddSpring(2, 6, Spring.Type.Muscle);//
            this.AddSpring(2, 7, Spring.Type.Cross);

            this.AddSpring(3, 4, Spring.Type.Normal);
            this.AddSpring(3, 6, Spring.Type.Cross);
            this.AddSpring(3, 7, Spring.Type.Muscle);//
            this.AddSpring(3, 8, Spring.Type.Cross);

            this.AddSpring(4, 5, Spring.Type.Cross);
            this.AddSpring(4, 7, Spring.Type.Cross);
            this.AddSpring(4, 8, Spring.Type.Muscle);//
            //---------------------------------

            this.AddSpring(5, 6, Spring.Type.Normal);
            this.AddSpring(5, 7, Spring.Type.Cross);
            this.AddSpring(5, 8, Spring.Type.Normal);
            this.AddSpring(5, 9, Spring.Type.Muscle);
            this.AddSpring(5, 10, Spring.Type.Cross);
            this.AddSpring(5, 12, Spring.Type.Cross);

            this.AddSpring(6, 7, Spring.Type.Normal);
            this.AddSpring(6, 8, Spring.Type.Cross);
            this.AddSpring(6, 9, Spring.Type.Cross);
            this.AddSpring(6, 10, Spring.Type.Muscle);
            this.AddSpring(6, 11, Spring.Type.Cross);

            this.AddSpring(7, 8, Spring.Type.Normal);
            this.AddSpring(7, 10, Spring.Type.Cross);
            this.AddSpring(7, 11, Spring.Type.Muscle);
            this.AddSpring(7, 12, Spring.Type.Cross);

            this.AddSpring(8, 9, Spring.Type.Cross);
            this.AddSpring(8, 11, Spring.Type.Cross);
            this.AddSpring(8, 12, Spring.Type.Muscle);
            //---------------------------------

            this.AddSpring(9, 10, Spring.Type.Normal);
            this.AddSpring(9, 11, Spring.Type.Cross);
            this.AddSpring(9, 12, Spring.Type.Normal);
            this.AddSpring(9, 13, Spring.Type.Muscle);
            this.AddSpring(9, 14, Spring.Type.Cross);
            this.AddSpring(9, 16, Spring.Type.Cross);

            this.AddSpring(10, 11, Spring.Type.Normal);
            this.AddSpring(10, 12, Spring.Type.Cross);
            this.AddSpring(10, 13, Spring.Type.Cross);
            this.AddSpring(10, 14, Spring.Type.Muscle);
            this.AddSpring(10, 15, Spring.Type.Cross);

            this.AddSpring(11, 12, Spring.Type.Normal);
            this.AddSpring(11, 14, Spring.Type.Cross);
            this.AddSpring(11, 15, Spring.Type.Muscle);
            this.AddSpring(11, 16, Spring.Type.Cross);

            this.AddSpring(12, 13, Spring.Type.Cross);
            this.AddSpring(12, 15, Spring.Type.Cross);
            this.AddSpring(12, 16, Spring.Type.Muscle);
            //--------------------------------

            this.AddSpring(13, 14, Spring.Type.Normal);
            this.AddSpring(13, 15, Spring.Type.Cross);
            this.AddSpring(13, 16, Spring.Type.Normal);
            this.AddSpring(13, 17, Spring.Type.Normal);
            this.AddSpring(13, 18, Spring.Type.Cross);
            this.AddSpring(13, 20, Spring.Type.Cross);

            this.AddSpring(14, 15, Spring.Type.Normal);
            this.AddSpring(14, 16, Spring.Type.Cross);
            this.AddSpring(14, 17, Spring.Type.Cross);
            this.AddSpring(14, 18, Spring.Type.Normal);
            this.AddSpring(14, 19, Spring.Type.Cross);

            this.AddSpring(15, 16, Spring.Type.Normal);
            this.AddSpring(15, 18, Spring.Type.Cross);
            this.AddSpring(15, 19, Spring.Type.Normal);
            this.AddSpring(15, 20, Spring.Type.Cross);

            this.AddSpring(16, 17, Spring.Type.Cross);
            this.AddSpring(16, 19, Spring.Type.Cross);
            this.AddSpring(16, 20, Spring.Type.Normal);
            //---------------------------------

            this.AddSpring(17, 18, Spring.Type.Normal);
            this.AddSpring(17, 19, Spring.Type.Cross);
            this.AddSpring(17, 20, Spring.Type.Normal);
            this.AddSpring(17, 21, Spring.Type.Normal);
            this.AddSpring(17, 22, Spring.Type.Cross);

            this.AddSpring(18, 19, Spring.Type.Normal);
            this.AddSpring(18, 20, Spring.Type.Cross);
            this.AddSpring(18, 21, Spring.Type.Normal);
            this.AddSpring(18, 22, Spring.Type.Cross);

            this.AddSpring(19, 20, Spring.Type.Normal);
            this.AddSpring(19, 21, Spring.Type.Cross);
            this.AddSpring(19, 22, Spring.Type.Normal);

            this.AddSpring(20, 21, Spring.Type.Cross);
            this.AddSpring(20, 22, Spring.Type.Normal);

            this.AddSpring(21, 22, Spring.Type.Normal);

        }

        protected void AddSpring(int from, int to, Spring.Type type, float k = 0.1f)
        {
            var nodes = this.fishGraph.Nodes.ToList();
            var s = new Spring(type);
            s.Left = nodes[from];
            s.Right = nodes[to];

            s.k = k;
            s.l = math.length(nodes[from].Position - nodes[to].Position);

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

                        if (edge.type == Spring.Type.Muscle) Gizmos.color = Color.red;
                        else if (edge.type == Spring.Type.Cross) Gizmos.color = Color.blue;
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

                var u_ij = j.velocity - i.velocity;
                var r_dot = (u_ij * r) / r_ij;

                var force_ij = (((s_ij.c * e_ij) + (s_ij.k * r_dot))/ r_ij) * r;
                ret += force_ij;
            }

            return ret;
        }

    }
}
