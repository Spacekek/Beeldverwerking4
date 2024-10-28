using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace INFOIBV
{
    public class Sift
    {
        // scale space
        int Q = 3;
        int P = 4;
        double sigma_s = 0.5;
        double sigma_0 = 1.6;
        double t_Extrm = 0.0;
        // Keypoint detection
        int n_Orient = 36;
        int n_Refine = 2;
        double reMax = 10.0;
        double t_DomOr = 0.8;
        double t_Mag = 0.01;
        double t_Peak = 0.01;
        // Feature descriptor
        int n_Spat = 4;
        int n_Angl = 16;
        double s_Desc = 10.0;
        double s_Fscale = 512.0;
        double t_Fclip = 0.2;
        // Feature matching
        double rmMax = 0.8;
        
        // GetSiftFeatures takes as input a greyscale image and returns the image
        public void GetSiftFeatures(byte[,] image)
        {
            (byte[][][,] G, int[][][,] D) = BuildSiftScaleSpace(image, sigma_s, sigma_0, P, Q);
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
        public (byte[][][,], int[][][,]) BuildSiftScaleSpace(byte[,] InputImage, double sigmaS, double sigma0, float P, float Q)
        {
            double initial_sigma = sigma0 * Math.Pow(2, (-1.0 / Q));
            float initial_increment_sigma = (float)Math.Sqrt((initial_sigma * initial_sigma) - (sigmaS * sigmaS));

            //filter size moet vgm mee groeien. miss kijken of int kunnen maken 
            byte filtersize = (byte)(6 * initial_increment_sigma + 1);
            if (filtersize % 2 == 0) { filtersize += 1; } //zorgt dat filtersize altijd oneven is

            float[,] gausianFilter = ImageOperations.createGaussianFilter(filtersize, initial_increment_sigma);  //maakt the gaussian filter

            byte[,] gausianFilteredImage = ImageOperations.convolveImage(InputImage, gausianFilter); //past gaussian filter toe op Inputimage

            byte[][,] firstOctave = new byte[(int)Q][,];
            firstOctave = MakeGaussianOctave(gausianFilteredImage, Q, sigma0); //maakt eerste octave van Q lang die gaussianfilter toepast op image met verschillende a 


            byte[][][,] octaves = new byte[(int)P][][,];
            octaves[0] = firstOctave;



            for (int p = 1; p < P; p++)
            {
                octaves[p] = MakeGaussianOctave(Decimate(octaves[p - 1][(int)Q - 1]), Q, sigma0);
            }

            int[][][,] DOGoctaves = new int[(int)P][][,];

            for (int j = 0; j < P; j++)
            {
                DOGoctaves[j] = MakeDogOctave(octaves[j]);
            }

            return (octaves, DOGoctaves);
        }

        private byte[][,] MakeGaussianOctave(byte[,] gaussianFilteredImage, float Q, double sigma0)
        {
            byte[][,] octave = new byte[(int)Q][,];
            float[,] gausianFilter;

            for (float i = 0; i < Q; i++)
            {
                float sd = (float)(sigma0 * Math.Sqrt(Math.Pow(2.0, (2.0 * i / Q)) - 1));

                byte filtersize = (byte)(6 * sd + 1); //past filter size aan opbasis van sd
                if (filtersize % 2 == 0) { filtersize += 1; } //zorgt dat filtersize altijd oneven is
                if (sd == 0) { octave[(int)i] = gaussianFilteredImage; }
                else
                {
                    gausianFilter = ImageOperations.createGaussianFilter(filtersize, sd);
                    octave[(int)i] = ImageOperations.convolveImage(gaussianFilteredImage, gausianFilter);
                }



            }
            return octave;

        }

        private int[][,] MakeDogOctave(byte[][,] octave)
        {
            byte[,] firstGaussian;
            byte[,] secondGaussian;

            int[][,] DOGoctave = new int[octave.GetLength(0) - 1][,];

            for (int i = 0; i < octave.Length - 1; i++)
            {
                firstGaussian = octave[i];
                secondGaussian = octave[i + 1];
                int[,] diffrenceGaussian = new int[firstGaussian.GetLength(0), firstGaussian.GetLength(1)];

                for (int x = 0; x < firstGaussian.GetLength(0); x++)
                {
                    for (int y = 0; y < firstGaussian.GetLength(1); y++)
                    {
                        diffrenceGaussian[x, y] = secondGaussian[x, y] - firstGaussian[x, y];

                    }

                }

                DOGoctave[i] = diffrenceGaussian;

            }

            return DOGoctave;

        }

        private byte[,] Decimate(byte[,] Image)
        {
            int breedte = Image.GetLength(0);
            int hoogte = Image.GetLength(1);


            breedte = breedte / 2;
            hoogte = hoogte / 2; //int floort

            byte[,] adjImage = new byte[breedte, hoogte];

            for (int i = 0; i < breedte; i++)
            {
                for (int j = 0; j < hoogte; j++)
                {
                    adjImage[i, j] = Image[2 * i, 2 * j];
                }
            }

            return adjImage;
        }

        // GetKeyPoints
        // input:
        // D: DoG scale space with P octaves containing Q levels
        
        // returns a set of keypoints located in D
        private List<Keypoint> GetKeyPoints(int[][][,] D)
        {
            List<Keypoint> C = new List<Keypoint>();
            for(int p = 0; p < D.GetLength(0); p++)
            {
               for(int q = 0; q < D[p].GetLength(0); q++)
               {
                 //List<Keypoint> E = FindExtrema(D, p, q);
                 //foreach(Keypoint k in E)
                 //    {
                 //        Keypoint k1 = RefineKeyPosition(D,k);
                 //        if (k1 != null)
                 //            { C.Add(k1); }
                 //    }
               }
            }
            return C;
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
        //private void GetScaleLevel(DoG D, int p, int q)
        //{
        //    //return D[p, q];
        //}

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
            if (center + t_Extrm < minimum)
                return true;
            if (center - t_Extrm > maximum)
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
        //    double alphamax = ((rho + 1)^2)/rho;
        //    Keypoint k2 = null;
        //    int n = 1;
        //    bool done = false;
        //    while (!done && n <= nrefine && IsInside(D, k))
        //    {
        //        byte[,,] N = GetNeighborhood(D, k);
        //        Matrix<double> delta = Gradient(N);
        //        Matrix<double> H = Hessian(N);
        //        if (H.Determinant() == 0)
        //        {
        //            done = true;
        //            return null;
        //        }
        //        Matrix<double> d = -H.Inverse()*delta;
        //        if (d[0,0].Magnitude() < 0.5 && d[1,0].Magnitude() < 0.5)
        //        {
        //            done = true;
        //            Matrix<double> Dpeak = N[1,1,1] + 0.5 * delta * d;
        //            // take top left 2x2 submatrix of H
        //            Matrix<double> Hxy = H.SubMatrix(0, 2, 0, 2);
        //            if (Dpeak.Magnitude() > tPeak && Hxy.Determinant() > 0)
        //            {
        //                k2= k + new Keypoint(0, 0, d[0,0], d[1,0]);
        //            }
        //            return k2;
        //        }
        //        // move to a neighboring DoG position at same level p, q:
        //        int u = (int)Math.Min(1, Math.Max(-1, Math.Round(d[0, 0])));
        //        int v = (int)Math.Min(1, Math.Max(-1, Math.Round(d[1, 0])));
        //        k = k + new Keypoint(0, 0, u, v);
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
        private Matrix<double> Gradient(byte[,,] N)
        {
           double[,] deltaarray = { { 0.5 * (N[1, 0, 0] - N[-1, 0, 0]), 0.5 * (N[0, 1, 0] - N[0, -1, 0]), 0.5 * (N[0, 0, 1] - N[0, 0, -1]) } };
           Matrix<double> delta = Matrix<double>.Build.DenseOfArray(deltaarray);
           return delta;
        }

        // Hessian(N)
        // input:
        // N, neighborhood

        // returns the estimated Hessian matrix of N
        private Matrix<double> Hessian(byte[,,] N)
        {
            double dxx = N[-1,0,0] - 2*N[0,0,0] + N[1,0,0];
            double dyy = N[0,-1,0] - 2*N[0,0,0] + N[0,1,0];
            double dss = N[0,0,-1] - 2*N[0,0,0] + N[0,0,1];
            double dxy = 0.25 * (N[1,1,0] - N[-1,1,0] - N[1,-1,0] + N[-1,-1,0]);
            double dxs = 0.25 * (N[1,0,1] - N[-1,0,1] - N[1,0,-1] + N[-1,0,-1]);
            double dys = 0.25 * (N[0,1,1] - N[0,-1,1] - N[0,1,-1] + N[0,-1,-1]);
            double[,] Harray = { {dxx, dxy, dxs}, {dxy, dyy, dys}, {dxs, dys, dss} };
            Matrix<double> H = Matrix<double>.Build.DenseOfArray(Harray);
            return H;
        }

        // GetDominantOrientations
        // input:
        // G, hierarchical Gaussian scale space
        // k', refined key point at octave p, scale lever q and spatial position x,y

        // returns a list of dominant orientations for the key point k'
        //private void GetDominantOrientations(List<int> G, Keypoint k)
        //{
        //    histogram h = GetOrientationHistogram(G, k);
        //    SmoothCircular(h, n);
        //    List<int> A = FindPeakOrientations(h); //list of dominant orientations 
        //}

        // MakeSiftDescriptor
        // input:
        // G, hierarchical gaussian scale space
        // k', refined key point
        // theta, dominant orientation

        // returns a new SIFT descriptor for the key point k'
        private void MakeSiftDescriptor()
        {

        }
        
        private void SmoothCircular(List<double> x, int iter)
        {
            double[] h = { 0.25, 0.5, 0.25 };
            int n = x.Count;
            for (int i = 1; i == iter; i++)
            {
                double s = x[0];
                double p = x[n-1];
                
                for(int j = 0; j == n-2; j++)
                {
                    double c = x[j];
                    x[j] = (h[0] * p + h[1] * c + h[2] * x[j+1]);
                    p = c;
                }
                x[n - 1] = h[0] * p + h[1]*x[n-1] + h[2]*s;
            }

            return;
        }
        private List<float> FindPeakOrientations(float[] h)
        {
            int t = 500; //threshold
            int n = h.Length;
            float h_max = h.Max();
            List<float> A = new List<float>();

            for (int k = 0; k < n; k++)
            {
                float hc = h[k];
                if (hc > t * h_max)
                {
                    float hp = h[k-1] % n;
                    float hn = h[k+1] % n;
                    if (hc > hp && hc > hn)
                    {
                        float k_new = k + (hp - hn) / (2 * (hp - (2 * hc) + hn));
                        double theta = (k_new * 2 * Math.PI / n) % (2*Math.PI);
                        A.Add((float)theta);
                    }
                }
            }

            return A;
        }
       
        private void GetOrientationHistogram(double[,,] G, Keypoint k)
        {
            double [,] Gpq = GetScaleLevel(G, k.p, k.q);
            int row = Gpq.GetLength(0);
            int col = Gpq.GetLength(1);
            Dictionary<int, double> h = new Dictionary<int, double>();
            for (int i = 0; i < row; i++) 
            {
        }

        private
    }


    class Keypoint
    {
        public int p; //octave
        public int q; //scale level
        public int x; //spatial positon (x,y) (in octave's coördinates)
        public int y;
    }
}
