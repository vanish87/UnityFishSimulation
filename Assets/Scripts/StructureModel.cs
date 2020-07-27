using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityTools.Common;
using UnityTools.Debuging.EditorTool;

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
                MuscleFront,
                MuscleMiddle,
                MuscleBack,
                Normal,
            }

            public enum Side
            {
                Left,
                Right,
                None,
            }

            protected Dictionary<Type, float> elasticMap = new Dictionary<Type, float>()
            {
                {Type.Cross , 38f },
                {Type.MuscleFront, 28f },
                {Type.MuscleMiddle, 28f },
                {Type.MuscleBack, 28f },
                {Type.Normal, 30f },
            };

            public float c = 38;  // elasticity constant
            public float k = 0.1f;// viscosity constant
            public float lr = 1;   // rest length
            public float lc = 1;   // fully contracted length
            public float activation = 0;
            public Type type = Type.Normal;
            public Side side = Side.None;

            public Spring(Type type, Side side)
            {
                this.type = type;
                this.side = side;
                this.c = elasticMap[this.type];
            }

            public float CurrentL { get => math.lerp(this.lr, this.lc, this.activation); }

            public override string ToString()
            {
                return "1";
            }
        }

        [System.Serializable]
        public class NormalFace
        {
            [SerializeField] protected float3 normal;
            public List<MassPoint> nodeList = new List<MassPoint>();

            public float3 Normal
            {
                get
                {
                    this.CalNormal();
                    return this.normal;
                }
            }
            public NormalFace(MassPoint p1, MassPoint p2, MassPoint p3, MassPoint p4)
            {
                this.nodeList.Add(p1);
                this.nodeList.Add(p2);
                this.nodeList.Add(p3);
                if (p4 != null) this.nodeList.Add(p4);
            }
            public void OnGizmos(float length = 1)
            {
                var p1 = this.nodeList[0].Position;
                var p2 = this.nodeList[1].Position;
                var p3 = this.nodeList[2].Position;
                var p4 = this.nodeList.Count > 3 ? this.nodeList[3].Position : float3.zero;
                var mid = (p1+p2+p3+p4)/ this.nodeList.Count;
                using (new GizmosScope(Color.yellow, Matrix4x4.identity))
                {
                    Gizmos.DrawLine(mid, mid + this.Normal * length);
                }
            }

            public void ApplyForceToNode(float mu = 1)
            {
                var area = this.CalArea();
                var velocity = this.CalVelocity();
                var n = this.Normal;
                var force = -mu * area * math.length(velocity) * (math.dot(n, velocity) * n);
                force = math.min(0, force);

                var num = this.nodeList.Count;
                force /= num;

                foreach(var node in this.nodeList)
                {
                    node.force += force;
                }
            }

            protected void CalNormal()
            {
                var p1 = this.nodeList[0].Position;
                var p2 = this.nodeList[1].Position;
                var p3 = this.nodeList[2].Position;
                var v1 = p2 - p1;
                var v2 = p3 - p1;

                this.normal = math.normalize(math.cross(v2, v1));
            }

            protected float Area(float3 p1, float3 p2, float3 p3)
            {
                var v1 = p2 - p1;
                var v2 = p3 - p1;
                var cos = math.dot(math.normalize(v1), math.normalize(v2));
                var sin = math.sqrt(1 - cos * cos);
                return math.length(v1) * math.length(v2) * sin;
            }

            protected float CalArea()
            {
                var p1 = this.nodeList[0].Position;
                var p2 = this.nodeList[1].Position;
                var p3 = this.nodeList[2].Position;
                var p4 = this.nodeList.Count > 3 ? this.nodeList[3].Position : float3.zero;
                var num = this.nodeList.Count;
                if(num == 3)
                {
                    return this.Area(p1, p2, p3);
                }
                else
                {
                    Assert.IsTrue(num == 4);
                    return this.Area(p1, p2, p4) + this.Area(p3, p4, p2);
                }
            }
            protected float3 CalVelocity()
            {
                var v1 = this.nodeList[0].velocity;
                var v2 = this.nodeList[1].velocity;
                var v3 = this.nodeList[2].velocity;
                var v4 = this.nodeList.Count > 3 ? this.nodeList[3].velocity : float3.zero;
                var num = this.nodeList.Count;

                return (v1 + v2 + v3 + v4) / num;
            }
        }



        protected Dictionary<Spring.Type, Color> springColorMap = new Dictionary<Spring.Type, Color>()
        {
            {Spring.Type.Cross , Color.gray },
            {Spring.Type.MuscleFront, Color.red },
            {Spring.Type.MuscleMiddle, Color.green },
            {Spring.Type.MuscleBack, Color.blue },
            {Spring.Type.Normal, Color.cyan },
        };

        protected Graph<MassPoint, Spring> fishGraph = new Graph<MassPoint, Spring>(23);
        [SerializeField] protected List<NormalFace> normals = new List<NormalFace>();

        [SerializeField] protected List<MassPoint> runtimeList;
        [SerializeField] protected List<Spring> runtimeMuscleList;
        [SerializeField] protected List<Spring> runtimeSpringList;

        public List<Spring> GetSpringByType(Spring.Type type)
        {
            var ret = new List<Spring>();
            for (var r = 0; r < this.fishGraph.AdjMatrix.Size.x; ++r)
            {
                for (var c = 0; c < this.fishGraph.AdjMatrix.Size.y; ++c)
                {
                    var s = this.fishGraph.GetEdge(r, c);
                    if (s == null || s.type != type) continue;
                    if (ret.Contains(s)) continue;

                    ret.Add(s);
                }
            }

            return ret;
        }

        protected void Start()
        {
            this.Load();
            this.fishGraph.Print();

            this.runtimeList = this.fishGraph.Nodes.ToList();

            this.fishGraph.AdjMatrix.Clean();
            this.InitSprings();

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

            this.runtimeMuscleList = this.GetSpringByType(Spring.Type.MuscleBack);
            this.runtimeMuscleList.AddRange(this.GetSpringByType(Spring.Type.MuscleMiddle));
            this.runtimeMuscleList.AddRange(this.GetSpringByType(Spring.Type.MuscleFront));

            this.InitNormals();
        }        

        protected void InitNormals()
        {
            this.AddNormalFace(0, 1, 2);
            this.AddNormalFace(0, 2, 3);
            this.AddNormalFace(0, 3, 4);
            this.AddNormalFace(0, 4, 1);

            this.AddNormalFace(1, 5, 6, 2);
            this.AddNormalFace(2, 6, 7, 3);
            this.AddNormalFace(3, 7, 8, 4);
            this.AddNormalFace(4, 8, 5, 1);

            this.AddNormalFace(5, 9, 10, 6);
            this.AddNormalFace(6, 10, 11, 7);
            this.AddNormalFace(7, 11, 12, 8);
            this.AddNormalFace(8, 12, 9, 5);

            this.AddNormalFace(9, 13, 14, 10);
            this.AddNormalFace(10, 14, 15, 11);
            this.AddNormalFace(11, 15, 16, 12);
            this.AddNormalFace(12, 16, 13, 9);


            this.AddNormalFace(13, 17, 18, 14);
            this.AddNormalFace(14, 18, 19, 15);
            this.AddNormalFace(15, 19, 20, 16);
            this.AddNormalFace(16, 20, 17, 13);

            this.AddNormalFace(17, 21, 18);
            this.AddNormalFace(18, 21, 22, 19);
            this.AddNormalFace(19, 22, 20);
            this.AddNormalFace(20, 22, 21, 17);
        }

        protected void AddNormalFace(int p1, int p2, int p3, int p4 = -1)
        {
            var nodes = this.fishGraph.Nodes.ToList();
            this.normals.Add(new NormalFace(nodes[p1], nodes[p2], nodes[p3], p4<0?null:nodes[p4]));
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
            this.AddSpring(1, 5, Spring.Type.MuscleFront, Spring.Side.Left);//
            this.AddSpring(1, 6, Spring.Type.Cross);
            this.AddSpring(1, 8, Spring.Type.Cross);

            this.AddSpring(2, 3, Spring.Type.Normal);
            this.AddSpring(2, 4, Spring.Type.Cross);
            this.AddSpring(2, 5, Spring.Type.Cross);
            this.AddSpring(2, 6, Spring.Type.MuscleFront, Spring.Side.Right);//
            this.AddSpring(2, 7, Spring.Type.Cross);

            this.AddSpring(3, 4, Spring.Type.Normal);
            this.AddSpring(3, 6, Spring.Type.Cross);
            this.AddSpring(3, 7, Spring.Type.MuscleFront, Spring.Side.Right);//
            this.AddSpring(3, 8, Spring.Type.Cross);

            this.AddSpring(4, 5, Spring.Type.Cross);
            this.AddSpring(4, 7, Spring.Type.Cross);
            this.AddSpring(4, 8, Spring.Type.MuscleFront, Spring.Side.Left);//
            //---------------------------------

            this.AddSpring(5, 6, Spring.Type.Normal);
            this.AddSpring(5, 7, Spring.Type.Cross);
            this.AddSpring(5, 8, Spring.Type.Normal);
            this.AddSpring(5, 9, Spring.Type.MuscleMiddle, Spring.Side.Left);
            this.AddSpring(5, 10, Spring.Type.Cross);
            this.AddSpring(5, 12, Spring.Type.Cross);

            this.AddSpring(6, 7, Spring.Type.Normal);
            this.AddSpring(6, 8, Spring.Type.Cross);
            this.AddSpring(6, 9, Spring.Type.Cross);
            this.AddSpring(6, 10, Spring.Type.MuscleMiddle, Spring.Side.Right);
            this.AddSpring(6, 11, Spring.Type.Cross);

            this.AddSpring(7, 8, Spring.Type.Normal);
            this.AddSpring(7, 10, Spring.Type.Cross);
            this.AddSpring(7, 11, Spring.Type.MuscleMiddle, Spring.Side.Right);
            this.AddSpring(7, 12, Spring.Type.Cross);

            this.AddSpring(8, 9, Spring.Type.Cross);
            this.AddSpring(8, 11, Spring.Type.Cross);
            this.AddSpring(8, 12, Spring.Type.MuscleMiddle, Spring.Side.Left);
            //---------------------------------

            this.AddSpring(9, 10, Spring.Type.Normal);
            this.AddSpring(9, 11, Spring.Type.Cross);
            this.AddSpring(9, 12, Spring.Type.Normal);
            this.AddSpring(9, 13, Spring.Type.MuscleBack, Spring.Side.Left);
            this.AddSpring(9, 14, Spring.Type.Cross);
            this.AddSpring(9, 16, Spring.Type.Cross);

            this.AddSpring(10, 11, Spring.Type.Normal);
            this.AddSpring(10, 12, Spring.Type.Cross);
            this.AddSpring(10, 13, Spring.Type.Cross);
            this.AddSpring(10, 14, Spring.Type.MuscleBack, Spring.Side.Right);
            this.AddSpring(10, 15, Spring.Type.Cross);

            this.AddSpring(11, 12, Spring.Type.Normal);
            this.AddSpring(11, 14, Spring.Type.Cross);
            this.AddSpring(11, 15, Spring.Type.MuscleBack, Spring.Side.Right);
            this.AddSpring(11, 16, Spring.Type.Cross);

            this.AddSpring(12, 13, Spring.Type.Cross);
            this.AddSpring(12, 15, Spring.Type.Cross);
            this.AddSpring(12, 16, Spring.Type.MuscleBack, Spring.Side.Left);
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

        protected void AddSpring(int from, int to, Spring.Type type, Spring.Side side = Spring.Side.None, float k = 0.1f)
        {
            var nodes = this.fishGraph.Nodes.ToList();
            var s = new Spring(type, side);
            s.Left = nodes[from];
            s.Right = nodes[to];

            s.k = k;
            s.lr = math.length(nodes[from].Position - nodes[to].Position);
            s.lc = s.lr * 0.6f;

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

            //if(Input.GetKey(KeyCode.G))
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

                        Gizmos.color = this.springColorMap[edge.type];
                        
                        edge.OnGizmos();
                    }
                }
            }
            foreach (var n in this.fishGraph.Nodes) n.OnGizmos(50 * Unit.WorldMMToUnityUnit);

            foreach (var n in this.normals) n.OnGizmos(200 * Unit.WorldMMToUnityUnit);
        }

        protected void Step()
        {
            var dt = 0.005f;
            foreach(var n in this.fishGraph.Nodes)
            {
                var force = this.GetSpringForce(n);
                n.force = force;
            }

            this.ApplyFluidForce();

            foreach (var n in this.fishGraph.Nodes)
            {
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

                var e_ij = r_ij - s_ij.CurrentL;

                var u_ij = j.velocity - i.velocity;
                var r_dot = (u_ij * r) / r_ij;

                var force_ij = (((s_ij.c * e_ij) + (s_ij.k * r_dot))/ r_ij) * r;
                ret += force_ij;
            }

            return ret;
        }

        protected void ApplyFluidForce()
        {
            foreach(var face in this.normals)
            {
                face.ApplyForceToNode();
            }
        }

    }
}
