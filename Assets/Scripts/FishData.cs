using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityTools;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{
    [System.Serializable]
    public class FishModelData
    {
        [SerializeField, Range(0.01f, 1)] protected float damping = 0.05f;
        [SerializeField] protected NewGraphAdj<MassPoint, Spring> fishGraph = new NewGraphAdj<MassPoint, Spring>(23);
        [SerializeField] protected List<NormalFace> normalFace = new List<NormalFace>();

        public NewGraphAdj<MassPoint, Spring> FishGraph { get => this.fishGraph; set => this.fishGraph = value; }
        public List<NormalFace> FishNormalFace { get => this.normalFace; }
        public float Damping { get => this.damping; }

        public List<Spring> GetSpringByType(List<Spring.Type> types)
        {
            return this.FishGraph.Edges.Where(e => types.Contains(e.SpringType)).ToList();
        }

        public float3 GeometryCenter
        {
            get
            {
                var edges = this.GetSpringByType(new List<Spring.Type>() { Spring.Type.MuscleMiddle });
                var center = float3.zero;
                foreach (var n in edges)
                {
                    center += n.Start.Position;
                    center += n.End.Position;
                }
                return center / (edges.Count * 2);
            }
        }

        public float3 Velocity
        {
            get
            {
                var vel = float3.zero;
                foreach (var n in this.FishGraph.Nodes)
                {
                    vel += n.Velocity;
                }
                return vel / this.FishGraph.Nodes.ToList().Count;
            }
        }

        public float3 Direction { get => this.Head.Position - this.GeometryCenter; }

        public MassPoint Head { get => this.FishGraph.Nodes.ToList()[0]; }

        public void OnGizmos(Dictionary<Spring.Type, Color> springColorMap)
        {
            if (this.FishGraph != null)
            {
                foreach (var edge in this.FishGraph.Edges)
                {
                    using (new GizmosScope(springColorMap[edge.SpringType], Matrix4x4.identity))
                    {
                        edge.OnGizmos();
                    }
                }
                foreach (var n in this.FishGraph.Nodes)
                {
                    n.OnGizmos(50 * Unit.WorldMMToUnityUnit);
                    //Gizmos.DrawLine(n.Position, n.Position + n.Velocity);
                }

                foreach (var n in this.FishNormalFace) n.OnGizmos(200 * Unit.WorldMMToUnityUnit);

                //Gizmos.DrawLine(Vector3.zero, this.totalForce);

                using (new GizmosScope(Color.red, Matrix4x4.identity))
                {
                    Gizmos.DrawLine(this.GeometryCenter, this.GeometryCenter + this.Direction);
                    Gizmos.DrawLine(this.Head.Position, this.Head.Position + this.Velocity);
                }
            }
        }
    }

    public static class GeometryFunctions
    {
        private static object lockObj = new object();
        public static FishModelData fish;
        public static Dictionary<Spring.Type, Color> springColorMap = new Dictionary<Spring.Type, Color>()
        {
            {Spring.Type.Cross , Color.gray },
            {Spring.Type.MuscleFront, Color.red },
            {Spring.Type.MuscleMiddle, Color.green },
            {Spring.Type.MuscleBack, Color.blue },
            {Spring.Type.Normal, Color.cyan },
        };
        public static void Save(FishModelData fish, string fileName = "fish.model")
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            FileTool.Write(path, fish);
            LogTool.Log("Saved " + path);
        }
        public static FishModelData Load(string fileName = "fish.model")
        {
            lock(lockObj)
            {
                try
                {
                    var path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
                    if (fish == null) LogTool.Log("Loaded " + path);
                    fish = fish ?? FileTool.Read<FishModelData>(path);
                }
                catch (System.Exception e)
                {
                    LogTool.Log(e.ToString(), LogLevel.Error);

                    fish = new FishModelData();
                    InitNewFishModel(fish);
                }
                return fish.DeepCopy();
            }
        }

        public static void InitNewFishModel(FishModelData fish)
        {
            fish.FishGraph.Clear();
            InitNodes(fish);
            InitSprings(fish);
            InitNormals(fish);
        }

        public static void InitNodes(FishModelData fish)
        {
            var n = fish.FishGraph.Nodes.ToList();

            var m1 = 6.6f;
            var m2 = 1.1f;
            var m3 = 0.165f;
            var m4 = 11.0f;

            var width = 0;

            SetNode(n[0], new float3(0, 0, 0), m2);

            SetNode(n[1], new float3(-5,  2,  2 + width), m1);
            SetNode(n[2], new float3(-5,  2, -2 - width), m1);
            SetNode(n[3], new float3(-5, -2, -2 - width), m1);
            SetNode(n[4], new float3(-5, -2,  2 + width), m1);


            SetNode(n[5], new float3(-10,  3,  3 + width), m4);
            SetNode(n[6], new float3(-10,  3, -3 - width), m4);
            SetNode(n[7], new float3(-10, -3, -3 - width), m4);
            SetNode(n[8], new float3(-10, -3,  3 + width), m4);


            SetNode(n[9], new float3(-15,   2,  2 + width * 0.5f), m1);
            SetNode(n[10], new float3(-15,  2, -2 - width * 0.5f), m1);
            SetNode(n[11], new float3(-15, -2, -2 - width * 0.5f), m1);
            SetNode(n[12], new float3(-15, -2,  2 + width * 0.5f), m1);


            SetNode(n[13], new float3(-20,  1,  1), m2);
            SetNode(n[14], new float3(-20,  1, -1), m2);
            SetNode(n[15], new float3(-20, -1, -1), m2);
            SetNode(n[16], new float3(-20, -1,  1), m2);


            SetNode(n[17], new float3(-22,  0.8f,  0.8f), m2);
            SetNode(n[18], new float3(-22,  0.8f, -0.8f), m2);
            SetNode(n[19], new float3(-22, -0.8f, -0.8f), m2);
            SetNode(n[20], new float3(-22, -0.8f,  0.8f), m2);


            SetNode(n[21], new float3(-26,  3, 0), m3);
            SetNode(n[22], new float3(-26, -3, 0), m3);
        }

        public static void SetNode(MassPoint node, float3 pos, float mass)
        {
            node.Position = pos;
            node.Mass = mass;
        }

        public static void InitNormals(FishModelData fish)
        {
            
            AddNormalFace(fish, 0, 1, 2);
            AddNormalFace(fish, 0, 2, 3);
            AddNormalFace(fish, 0, 3, 4);
            AddNormalFace(fish, 0, 4, 1);

            AddNormalFace(fish, 1, 5, 6, 2);
            AddNormalFace(fish, 2, 6, 7, 3);
            AddNormalFace(fish, 3, 7, 8, 4);
            AddNormalFace(fish, 4, 8, 5, 1);

            AddNormalFace(fish, 5, 9, 10, 6);
            AddNormalFace(fish, 6, 10, 11, 7);
            AddNormalFace(fish, 7, 11, 12, 8);
            AddNormalFace(fish, 8, 12, 9, 5);

            AddNormalFace(fish, 9, 13, 14, 10);
            AddNormalFace(fish, 10, 14, 15, 11);
            AddNormalFace(fish, 11, 15, 16, 12);
            AddNormalFace(fish, 12, 16, 13, 9);


            AddNormalFace(fish, 13, 17, 18, 14);
            AddNormalFace(fish, 14, 18, 19, 15);
            AddNormalFace(fish, 15, 19, 20, 16);
            AddNormalFace(fish, 16, 20, 17, 13);

            AddNormalFace(fish, 17, 21, 18);
            AddNormalFace(fish, 18, 21, 22, 19);
            AddNormalFace(fish, 19, 22, 20);
            AddNormalFace(fish, 20, 22, 21, 17);
        }

        public static void AddNormalFace(FishModelData fish, int p1, int p2, int p3, int p4 = -1)
        {
            var nodes = fish.FishGraph.Nodes.ToList();
            fish.FishNormalFace.Add(new NormalFace(nodes[p1], nodes[p2], nodes[p3], p4 < 0 ? null : nodes[p4]));
        }

        public static void InitSprings(FishModelData fish)
        {
            AddSpring(fish, 0, 1, Spring.Type.Normal);
            AddSpring(fish, 0, 2, Spring.Type.Normal);
            AddSpring(fish, 0, 3, Spring.Type.Normal);
            AddSpring(fish, 0, 4, Spring.Type.Normal);

            AddSpring(fish, 1, 2, Spring.Type.Normal);
            AddSpring(fish, 1, 3, Spring.Type.Cross);
            AddSpring(fish, 1, 4, Spring.Type.Normal);
            AddSpring(fish, 1, 5, Spring.Type.MuscleFront, Spring.Side.Left);//
            AddSpring(fish, 1, 6, Spring.Type.Cross);
            AddSpring(fish, 1, 8, Spring.Type.Cross);

            AddSpring(fish, 2, 3, Spring.Type.Normal);
            AddSpring(fish, 2, 4, Spring.Type.Cross);
            AddSpring(fish, 2, 5, Spring.Type.Cross);
            AddSpring(fish, 2, 6, Spring.Type.MuscleFront, Spring.Side.Right);//
            AddSpring(fish, 2, 7, Spring.Type.Cross);

            AddSpring(fish, 3, 4, Spring.Type.Normal);
            AddSpring(fish, 3, 6, Spring.Type.Cross);
            AddSpring(fish, 3, 7, Spring.Type.MuscleFront, Spring.Side.Right);//
            AddSpring(fish, 3, 8, Spring.Type.Cross);

            AddSpring(fish, 4, 5, Spring.Type.Cross);
            AddSpring(fish, 4, 7, Spring.Type.Cross);
            AddSpring(fish, 4, 8, Spring.Type.MuscleFront, Spring.Side.Left);//
            //---------------------------------

            AddSpring(fish, 5, 6, Spring.Type.Normal);
            AddSpring(fish, 5, 7, Spring.Type.Cross);
            AddSpring(fish, 5, 8, Spring.Type.Normal);
            AddSpring(fish, 5, 9, Spring.Type.MuscleMiddle, Spring.Side.Left);
            AddSpring(fish, 5, 10, Spring.Type.Cross);
            AddSpring(fish, 5, 12, Spring.Type.Cross);

            AddSpring(fish, 6, 7, Spring.Type.Normal);
            AddSpring(fish, 6, 8, Spring.Type.Cross);
            AddSpring(fish, 6, 9, Spring.Type.Cross);
            AddSpring(fish, 6, 10, Spring.Type.MuscleMiddle, Spring.Side.Right);
            AddSpring(fish, 6, 11, Spring.Type.Cross);

            AddSpring(fish, 7, 8, Spring.Type.Normal);
            AddSpring(fish, 7, 10, Spring.Type.Cross);
            AddSpring(fish, 7, 11, Spring.Type.MuscleMiddle, Spring.Side.Right);
            AddSpring(fish, 7, 12, Spring.Type.Cross);

            AddSpring(fish, 8, 9, Spring.Type.Cross);
            AddSpring(fish, 8, 11, Spring.Type.Cross);
            AddSpring(fish, 8, 12, Spring.Type.MuscleMiddle, Spring.Side.Left);
            //---------------------------------

            AddSpring(fish, 9, 10, Spring.Type.Normal);
            AddSpring(fish, 9, 11, Spring.Type.Cross);
            AddSpring(fish, 9, 12, Spring.Type.Normal);
            AddSpring(fish, 9, 13, Spring.Type.MuscleBack, Spring.Side.Left);
            AddSpring(fish, 9, 14, Spring.Type.Cross);
            AddSpring(fish, 9, 16, Spring.Type.Cross);

            AddSpring(fish, 10, 11, Spring.Type.Normal);
            AddSpring(fish, 10, 12, Spring.Type.Cross);
            AddSpring(fish, 10, 13, Spring.Type.Cross);
            AddSpring(fish, 10, 14, Spring.Type.MuscleBack, Spring.Side.Right);
            AddSpring(fish, 10, 15, Spring.Type.Cross);

            AddSpring(fish, 11, 12, Spring.Type.Normal);
            AddSpring(fish, 11, 14, Spring.Type.Cross);
            AddSpring(fish, 11, 15, Spring.Type.MuscleBack, Spring.Side.Right);
            AddSpring(fish, 11, 16, Spring.Type.Cross);

            AddSpring(fish, 12, 13, Spring.Type.Cross);
            AddSpring(fish, 12, 15, Spring.Type.Cross);
            AddSpring(fish, 12, 16, Spring.Type.MuscleBack, Spring.Side.Left);
            //--------------------------------

            AddSpring(fish, 13, 14, Spring.Type.Normal);
            AddSpring(fish, 13, 15, Spring.Type.Cross);
            AddSpring(fish, 13, 16, Spring.Type.Normal);
            AddSpring(fish, 13, 17, Spring.Type.Normal);
            AddSpring(fish, 13, 18, Spring.Type.Cross);
            AddSpring(fish, 13, 20, Spring.Type.Cross);

            AddSpring(fish, 14, 15, Spring.Type.Normal);
            AddSpring(fish, 14, 16, Spring.Type.Cross);
            AddSpring(fish, 14, 17, Spring.Type.Cross);
            AddSpring(fish, 14, 18, Spring.Type.Normal);
            AddSpring(fish, 14, 19, Spring.Type.Cross);

            AddSpring(fish, 15, 16, Spring.Type.Normal);
            AddSpring(fish, 15, 18, Spring.Type.Cross);
            AddSpring(fish, 15, 19, Spring.Type.Normal);
            AddSpring(fish, 15, 20, Spring.Type.Cross);

            AddSpring(fish, 16, 17, Spring.Type.Cross);
            AddSpring(fish, 16, 19, Spring.Type.Cross);
            AddSpring(fish, 16, 20, Spring.Type.Normal);
            //---------------------------------

            AddSpring(fish, 17, 18, Spring.Type.Normal);
            AddSpring(fish, 17, 19, Spring.Type.Cross);
            AddSpring(fish, 17, 20, Spring.Type.Normal);
            AddSpring(fish, 17, 21, Spring.Type.Normal);
            AddSpring(fish, 17, 22, Spring.Type.Cross);

            AddSpring(fish, 18, 19, Spring.Type.Normal);
            AddSpring(fish, 18, 20, Spring.Type.Cross);
            AddSpring(fish, 18, 21, Spring.Type.Normal);
            AddSpring(fish, 18, 22, Spring.Type.Cross);

            AddSpring(fish, 19, 20, Spring.Type.Normal);
            AddSpring(fish, 19, 21, Spring.Type.Cross);
            AddSpring(fish, 19, 22, Spring.Type.Normal);

            AddSpring(fish, 20, 21, Spring.Type.Cross);
            AddSpring(fish, 20, 22, Spring.Type.Normal);

            AddSpring(fish, 21, 22, Spring.Type.Normal);
        }

        public static void AddSpring(FishModelData fish, int from, int to, Spring.Type type, Spring.Side side = Spring.Side.None)
        {
            var nodes = fish.FishGraph.Nodes.ToList();
            var s = new Spring(type, side, nodes[from], nodes[to]);

            fish.FishGraph.AddEdge(from, to, s);
        }
    }

    [System.Serializable]
    public class MassPoint : Point, INode
    {
        [SerializeField] protected int id;
        [SerializeField] protected float mass;

        //runtime
        [SerializeField] protected float3 force;
        [SerializeField] protected float3 velocity;

        public int Index { get => this.id; set => this.id = value; }
        public float Mass { get => this.mass; internal set => this.mass = value; }
        public float3 Force { get => this.force; set => this.force = value; }
        public float3 Velocity { get => this.velocity; set => this.velocity = value; }
    }

    [System.Serializable]
    public class Spring : PointSegment<MassPoint>, IEdge
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

        protected static Dictionary<Spring.Type, float> elasticMap = new Dictionary<Spring.Type, float>()
        {
            {Spring.Type.Cross , 38f },
            {Spring.Type.MuscleFront, 28f },
            {Spring.Type.MuscleMiddle, 28f },
            {Spring.Type.MuscleBack, 28f },
            {Spring.Type.Normal, 30f },
        };

        protected static Dictionary<Spring.Type, float> viscosityMap = new Dictionary<Spring.Type, float>()
        {
            {Spring.Type.Cross , 0.1f },
            {Spring.Type.MuscleFront, 0.1f },
            {Spring.Type.MuscleMiddle, 0.1f },
            {Spring.Type.MuscleBack, 0.1f },
            {Spring.Type.Normal, 0.1f },
        };

        [SerializeField] protected float c = 38;  // elasticity constant
        [SerializeField] protected float k = 0.1f;// viscosity constant
        [SerializeField] protected float lr = 1;   // rest length
        [SerializeField] protected float lc = 1;   // fully contracted length
        [SerializeField] protected float activation = 0;
        [SerializeField] protected Type type = Type.Normal;
        [SerializeField] protected Side side = Side.None;
        public float Activation { get => this.activation; set => this.activation = value; }
        public float CurrentL { get => math.lerp(this.lr, this.lc, this.Activation); }
        public float C { get => this.c; }
        public float K { get => this.k; }
        public Type SpringType { get => this.type; }
        public Side SpringSide { get => this.side; }

        public Spring(Type type, Side side, MassPoint from, MassPoint to)
        {
            this.Start = from;
            this.End = to;

            this.type = type;
            this.side = side;
            this.c = elasticMap[this.type];
            this.k = viscosityMap[this.type];

            this.activation = 0.5f;
            var currentL = math.length(from.Position - to.Position);
            var full = currentL / this.activation;

            var ratio = 0.65f;
            this.lr = full * ratio;
            this.lc = full * (1 - ratio);

            //if (type == Spring.Type.MuscleBack) this.lc = this.lr * 0.3f;
        }


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

        float3 force;
        public void OnGizmos(float length = 1)
        {
            var p1 = this.nodeList[0].Position;
            var p2 = this.nodeList[1].Position;
            var p3 = this.nodeList[2].Position;
            var p4 = this.nodeList.Count > 3 ? this.nodeList[3].Position : float3.zero;
            var mid = (p1 + p2 + p3 + p4) / this.nodeList.Count;
            using (new GizmosScope(Color.yellow, Matrix4x4.identity))
            {
                //Gizmos.DrawLine(mid, mid + this.Normal * length);
            }
            using (new GizmosScope(Color.gray, Matrix4x4.identity))
            {
                Gizmos.DrawLine(mid, mid + this.force * length);
            }

            using (new GizmosScope(Color.red, Matrix4x4.identity))
            {
                //Gizmos.DrawLine(mid, mid + this.vproj * length);
            }
        }

        public void ApplyForceToNode(float mu = 1)
        {
            var area = this.CalArea();
            var velocity = this.CalVelocity();
            var waterVelocity = float3.zero;
            var v = velocity - waterVelocity;
            var n = this.Normal;
            var force = -mu * area * math.length(v) * (math.dot(n, v) * n);
            //force = math.min(0, force);

            var anlge = math.dot(n, math.normalize(force));
            if (anlge > 0) force = 0;

            var num = this.nodeList.Count;
            force /= num;

            this.force = force;
            foreach (var node in this.nodeList)
            {
                node.Force += force;
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
            return 0.5f * math.length(v1) * math.length(v2) * sin;
        }

        protected float CalArea()
        {
            var p1 = this.nodeList[0].Position;
            var p2 = this.nodeList[1].Position;
            var p3 = this.nodeList[2].Position;
            var p4 = this.nodeList.Count > 3 ? this.nodeList[3].Position : float3.zero;
            var num = this.nodeList.Count;
            if (num == 3)
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
            var v1 = this.nodeList[0].Velocity;
            var v2 = this.nodeList[1].Velocity;
            var v3 = this.nodeList[2].Velocity;
            var v4 = this.nodeList.Count > 3 ? this.nodeList[3].Velocity : float3.zero;
            var num = this.nodeList.Count;

            return (v1 + v2 + v3 + v4) / num;
        }
    }

}