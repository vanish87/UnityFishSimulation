using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Math;

namespace UnityTools.Algorithm
{
    public class DownhillSimplex<T> : IterationAlgorithm
    {
        public abstract class Problem : IProblem, Function<Vector<T>, float>
        {
            internal protected int dim;
            internal protected float eq = 0.00001f;

            public abstract float Evaluate(Vector<T> x);
            public abstract Vector<T> Generate(Vector<T> x);

            public Problem(int dim)
            {
                this.dim = dim;
            }
        }

        [System.Serializable]
        public class Vertice
        {
            public Vector<T> X 
            { get => this.x; 
                set 
                { 
                    this.x = value; 
                    this.fx = p.Evaluate(this.x); 
                } 
            }
            public float Fx { get { return this.fx; } }

            private float fx;
            private Vector<T> x;
            private Problem p;

            public Vertice(Problem problem)
            {
                this.p = problem;

                this.X  = this.p.Generate(this.x);
            }

            public void Print()
            {
                Debug.Log(x[0] + " " + x[1] + " Fx= " + fx);
            }
        }

        [System.Serializable]
        public class Solution : ISolution
        {
            public Vertice min;
        }
        public List<Vertice> Vertices { get => this.simplex; }

        protected List<Vertice> simplex = new List<Vertice>();

        public DownhillSimplex(IProblem problem, IDelta dt) : base(problem, dt)
        {
            var p = problem as Problem;
            var vn = p.dim + 1;
            foreach(var r in Enumerable.Range(0, vn))
            {
                this.simplex.Add(new Vertice(p));
            }

            this.currentSolution = new Solution();
        }

        public override bool IsSolutionAcceptable(ISolution solution)
        {
            var vn = this.simplex.Count;
            var n = vn - 1;
            var mean = new Vector<T>(n);
            foreach (var v in this.simplex)
            {
                mean = mean + v.X;
            }
            mean = mean / vn;

            var sd = 0f;
            foreach (var v in this.simplex)
            {
                var d = v.X - mean;
                for (var i = 0; i < d.Size; ++i)
                {
                    dynamic x = d[i];
                    sd += x * x;
                }
            }

            sd /= vn;

            var p = this.problem as Problem;
            if (sd < p.eq)
            {
                LogTool.Log("Down with sd " + sd);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override ISolution Solve(IProblem problem)
        {
            var p = problem as Problem;
            var vn = this.simplex.Count;
            var n = vn - 1;
            var ordered = this.simplex.OrderBy(v => v.Fx).ToList();
            
            var mean = new Vector<T>(n);
            for (var i = 0; i < n; ++i)
            {
                mean = mean + ordered[i].X;
            }
            mean = mean / n;


            var worst = ordered[n];
            var xn1 = worst.X;
            
            var xr = mean + 1f * (mean - xn1);

            var f1  = ordered[0].Fx;
            var fxr = p.Evaluate(xr);
            var fn  = ordered[n].Fx;

            if (f1 < fxr && fxr < fn)
            {
                worst.X = xr;
            }
            else
            if (fxr < f1)
            {

                var xe = xr + 1f * (xr - mean);

                var fe = p.Evaluate(xe);
                if (fe < fxr)
                {
                    worst.X = xe;
                }
                else
                {
                    worst.X = xr;
                }
            }
            else
            if (fxr > fn)
            {
                var xc = mean + 0.5f * (xn1 - mean);

                var fc = p.Evaluate(xc);
                var fn1 = worst.Fx;
                if (fc < fn1)
                {
                    worst.X = xc;
                }
                else
                {
                    var best = ordered[0];
                    for (var i = 1; i < vn; ++i)
                    {
                        ordered[i].X = best.X + 0.5f * (ordered[i].X - best.X);
                    }
                }
            }

            var sol = this.CurrentSolution as Solution;
            sol.min = ordered.FirstOrDefault();

            if (this.IsSolutionAcceptable(sol))
            {
                this.ChangeState(SimulatorSateDone.Instance);
            }


            return sol;
        }
    }
}