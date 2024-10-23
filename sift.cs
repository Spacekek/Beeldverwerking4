using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOIBV
{
    internal class Sift
    {
        byte tExtrm = 1;
        // GetSiftFeatures takes as input a greyscale image and returns the image
        public void GetSiftFeatures(byte[,] image)
        {
            // G, D = BuildSiftScaleSpace()
            // C = GetKeyPoints(D)
            // S = new list sift descriptors
            //foreach(k in C)
            //{
            //    orientations = GetDominantOrientations();
            //    foreach(theta in orientations)
            //    {
            //        s = MakeSiftDescriptor();
            //        S.add(s);
            //    }
            //}
            //return S;
        }

        // BuildSiftScaleSpace
        // Input:
        // I, image
        // sigmaS, sampling scale
        // sigma0, reference scale of the first octave
        // P, number of octaves
        // Q, number of scale steps per octave

        // returns a sift scale space representation (G, D) of the image
        // G: a Hierarchical gaussian scale space
        // D: a hierarchical DoG scale space
        private void BuildSiftScaleSpace(byte[,] image, float sigmaS, float sigma0, int P, int Q)
        {

        }

        // GetKeyPoints
        // input:
        // D: DoG scale space with P octaves containing Q levels
        
        // returns a set of keypoints located in D
        private void GetKeyPoints()
        {
            //List<Keypoint> C = new list<Keypoint>;
            //for(int p = 0; p < D.P; p++)
            //{
            //    for(int q = 0; q < D.Q; q++)
            //    {
            //      E = FindExtrema(D, p, q);
            //      foreach(keypoint k in E)
            //          {
            //              k' = RefineKeyPosition(D,k);
            //              if (k' != null)
            //                  { C.Add(k') }
            //          }
            //    }
            //}
            //return C
        }

        // FindExtrema
        // input:
        // D: DoG scale space
        // p: octave
        // q: step

        // returns list of extrema
        private void FindExtrema()
        {
            // d = GetScaleLevel(D, p, q);
            //int M, N = d.size;
            //List<Keypoint> E = new List<Keypoint>(); // empty list of extrema
            //for (int u = 0; u < M-1; u++)
            //{
            //    for (int v = 0; v < N-1; v++)
            //    {
            //        if (d.length() > tmag)
            //        {
            //            k = (p, q, u, v);
            //            N = GetNeighborhood(D, k);
            //            if (IsExtremum(N))
            //            {
            //                E.Add(k);
            //            }
            //        }
            //    }
            //}
            //return E;
        }

        // GetScaleLevel(D, p, q)
        // input:
        // D: DoG scale space
        // p: octave
        // q: step

        // returns the specified scale level
        private void GetScaleLevel()
        {
            //return D[p, q];
        }

        // GetNeighborhood
        // input:
        // D: DoG scale space
        // k: keypoint

        // returns 3x3x3 neighborhood values around position k in D
        //private byte[,,] GetNeighborhood(DoG D, Keypoint k)
        //{
        //    byte[,,] N = new byte[3, 3, 3];
        //    for (int u = -1; u > 1; u++)
        //    {
        //        for (int v = -1; v > 1; v++)
        //        {
        //            for (int w  = -1; w > 1; w++)
        //            {
        //                N[u + 1, v + 1, w + 1] = D[k.p, k.q + w](k.x + u, k.y + v);
        //            }
        //        }
        //    }
        //    return N;
        //}

        // IsExtremum(N)
        // input:
        // N, neighborhood

        // returns whether N is local minimum or maximum by the threshold tExtrm >=0
        private bool IsExtremum(byte[,,] N)
        {
            byte center = N[1, 1, 1];
            byte minimum = 255;
            byte maximum = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        if (N[i, j, k] < minimum)
                            minimum = N[i, j, k];
                        if (N[i, j, k] > maximum)
                            maximum = N[i, j, k];
                    }
                }
            }
            if (center + tExtrm < minimum)
                return true;
            if (center - tExtrm > maximum)
                return true;
            return false;
        }

        // RefineKeyPosition(D, k)
        // input:
        // D, DoG scale space
        // k, candidate position

        // returns a refined key point k'or null if no key point could be localized at or near k
        //private Keypoint RefineKeyPosition(DoG D, Keypoint k)
        //{
        //    alphamax = ((rho + 1)^2)/rho;
        //    Keypoint k' = null;
        //    int n = 1;
        //    bool done = false;
        //    while (!done && n <= nrefine && IsInside(D, k))
        //    {
        //        byte[,,] N = GetNeighborhood(D, k);
        //        delta = Gradient(N);
        //        H = Hessian(N);
        //        if (Determinant(H) == 0)
        //        {
        //            done = true;
        //            return null;
        //        }
        //        d = -Inverse(H)*delta;
        //        if (Length(d.X) < 0.5 && Length(d.Y) < 0.5)
        //        {
        //            done = true;
        //            Dpeak = N[1,1,1] + 0.5 * Transpose(delta) * d;
        //            // take top left 2x2 submatrix of H
        //            Hxy = H[0:2, 0:2];
        //            if (Length(Dpeak) > tPeak && Determinant(Hxy) > 0)
        //            {
        //                k'= k + (0, 0, d.X, d.Y);
        //            }
        //            return k';
        //        }
        //        // move to a neighboring DoG position at same level p, q:
        //        u' = min(1, max(-1, round(d.X)))
        //        v' = min(1, max(-1, round(d.Y)))
        //        k = k + (0, 0, u', v');
        //        n = n + 1;
        //    }
        //    return null;
        //}

        // IsInside(D, k)
        // input:
        // D, DoG scale space
        // k, keypoint

        // returns whether k is inside D
        //private bool IsInside(DoG D, Keypoint k)
        //{
        //    p, q, u, v = k;
        //    M, N = GetScaleLevel(D, p, q).size;
        //    return (0 < u < M-1) && (0 < v < N-1) && (0 <= q < Q);
        //}

        // Gradient(N)
        // input:
        // N, neighborhood

        // returns the estimated gradient of N
        //private Matrix Gradient(byte[,] N)
        //{
        //    Matrix delta = 0.5 * (N[1,0,0] - N[-1,0,0], N[0,1,0] - N[0,-1,0], N[0,0,1] - N[0,0,-1]);
        //    return delta;
        //}

        // Hessian(N)
        // input:
        // N, neighborhood

        // returns the estimated Hessian matrixof N
        //private Matrix Hessian(byte[,] N)
        //{
        //    float dxx = N[-1,0,0] - 2*N[0,0,0] + N[1,0,0];
        //    float dyy = N[0,-1,0] - 2*N[0,0,0] + N[0,1,0];
        //    float dss = N[0,0,-1] - 2*N[0,0,0] + N[0,0,1];
        //    float dxy = 0.25 * (N[1,1,0] - N[-1,1,0] - N[1,-1,0] + N[-1,-1,0]);
        //    float dxs = 0.25 * (N[1,0,1] - N[-1,0,1] - N[1,0,-1] + N[-1,0,-1]);
        //    float dys = 0.25 * (N[0,1,1] - N[0,-1,1] - N[0,1,-1] + N[0,-1,-1]);
        //    Matrix H = (dxx, dxy, dxs, dxy, dyy, dys, dxs, dys, dss);
        //    return H;
        //}

        // GetDominantOrientations
        // input:
        // G, hierarchical Gaussian scale space
        // k', refined key point at octave p, scale lever q and spatial position x,y

        // returns a list of dominant orientations for the key point k'
        private void GetDominantOrientations()
        {

        }

        // MakeSiftDescriptor
        // input:
        // G, hierarchical gaussian scale space
        // k', refined key point
        // theta, dominant orientation

        // returns a new SIFT descriptor for the key point k'
        private void MakeSiftDescriptor()
        {

        }
    }
}
