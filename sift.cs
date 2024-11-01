using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        int n_Refine = 5;
        int n_smooth = 2;
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
        public List<SIFTdescriptor> GetSiftFeatures(byte[,] image)
        {
            (byte[][][,] G, int[][][,] D) = BuildSiftScaleSpace(image, sigma_s, sigma_0, P, Q);
            List<Keypoint> C = GetKeyPoints(D);
            List<SIFTdescriptor> S = new List<SIFTdescriptor>();
            foreach(Keypoint k in C)
            {
                List<float> orientations = GetDominantOrientations(G, k);
                foreach (float theta in orientations)
                {
                    SIFTdescriptor s = MakeSiftDescriptor(G, k, theta);
                    S.Add(s);
                }
            }
            return S;
        }

        // returns a sift scale space representation (G, D) of the image
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
                        diffrenceGaussian[x, y] = Math.Abs(secondGaussian[x, y] - firstGaussian[x, y]);

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

        // returns a set of keypoints located in D
        private List<Keypoint> GetKeyPoints(int[][][,] D)
        {
            List<Keypoint> C = new List<Keypoint>();
            for (int p = 0; p < D.GetLength(0); p++)
            {
               for(int q = 0; q < D[p].GetLength(0); q++)
               {
                 List<Keypoint> E = FindExtrema(D, p, q);
                 foreach(Keypoint k in E)
                    {
                        Keypoint k1 = RefineKeyPosition(D,k);
                        if (k1 != null)
                            { C.Add(k1); }
                    }
               }
            }
            return C;
        }

        // returns list of extrema
        private List<Keypoint> FindExtrema(int[][][,] D, int p, int q)
        {
            int[,] d = GetScaleLevel(D, p, q);
            int M = d.GetLength(0);
            int N = d.GetLength(1);
            List<Keypoint> E = new List<Keypoint>(); // empty list of extrema
            for (int u = 0; u < M - 1; u++)
            {
                for (int v = 0; v < N - 1; v++)
                {
                    if (Math.Abs(d[u,v]) > t_Mag)
                    {
                        Keypoint k = new Keypoint(p, q, u, v);
                        int[,,] neighborhood = GetNeighborhood(D, k);
                        if (IsExtremum(neighborhood))
                        {
                            E.Add(k);
                        }
                    }
                }
            }
            return E;
        }

        // returns the specified scale level
        private int[,] GetScaleLevel(int[][][,] D, int p, int q)
        {
            return D[p][q];
        }
        private byte[,] GetScaleLevel(byte[][][,] G, int p, int q)
        {
            return G[p][q];
        }

        // returns 3x3x3 neighborhood values around position k in D
        private int[,,] GetNeighborhood(int[][][,] D, Keypoint k)
        {
            int[,,] N = new int[3, 3, 3];
            for (int u = -1; u < 1; u++)
            {
                for (int v = -1; v < 1; v++)
                {
                    for (int w = -1; w < 1; w++)
                    {
                        if (k.q + w >= 0 && k.q + w < D[k.p].Length && 
                            k.x + u >= 0 && k.x + u < D[k.p][k.q + w].GetLength(0) &&
                            k.y + v >= 0 && k.y + v < D[k.p][k.q + w].GetLength(1)) {
                            N[u + 1, v + 1, w + 1] = D[k.p][k.q + w][k.x + u, k.y + v];
                        }
                    }
                }
            }
            return N;
        }

        // returns whether N is local minimum or maximum by the threshold tExtrm >=0
        private bool IsExtremum(int[,,] N)
        {
            int center = N[1, 1, 1];
            int minimum = int.MaxValue;
            int maximum = int.MinValue;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        if (i == 1 && j == 1 && k == 1)
                            continue;
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

        // returns a refined key point k'or null if no key point could be localized at or near k
        private Keypoint RefineKeyPosition(int[][][,] D, Keypoint k)
        {
            double alphamax = Math.Pow(reMax + 1, 2) / reMax;
            Keypoint k2 = null;
            int n = 1;
            bool done = false;
            while (!done && n <= n_Refine && IsInside(D, k))
            {
                int[,,] N = GetNeighborhood(D, k);
                Matrix<double> delta = Gradient(N);
                Matrix<double> H = Hessian(N);
                if (H.Determinant() == 0)
                {
                    done = true;
                    return null;
                }
                Matrix<double> d = -H.Inverse() * delta;
                if (d[0, 0].Magnitude() < 0.5 && d[1, 0].Magnitude() < 0.5)
                {
                    done = true;
                    Matrix<double> Dpeak = N[1, 1, 1] + 0.5 * delta.Transpose() * d;
                    // take top left 2x2 submatrix of H
                    Matrix<double> Hxy = H.SubMatrix(0, 2, 0, 2);
                    if (Math.Abs(Dpeak[0,0]) > t_Peak && Hxy.Determinant() > 0) // ASSUMES 1x1 Dpeak MATRIX MIGHT BE WRONG
                    {
                        k2 = k + new Keypoint(0, 0, (int)Math.Round(d[0, 0]), (int)Math.Round(d[1, 0]));
                    }
                    return k2;
                }
                // move to a neighboring DoG position at same level p, q:
                int u = (int)Math.Min(1, Math.Max(-1, Math.Round(d[0, 0])));
                int v = (int)Math.Min(1, Math.Max(-1, Math.Round(d[1, 0])));
                k = k + new Keypoint(0, 0, u, v);
                n++;
            }
            return null;
        }
        // returns whether k is inside D
        private bool IsInside(int[][][,] D, Keypoint k)
        {
            (int p, int q, int u, int v) = (k.p, k.q, k.x, k.y);
            (int M, int N) = (GetScaleLevel(D, p, q).GetLength(0), GetScaleLevel(D, p, q).GetLength(1));
            return (0 < u && u < M - 1) && (0 < v && v < N - 1) && (0 <= q && q < Q);
        }

        // returns the estimated gradient of N
        private Matrix<double> Gradient(int[,,] N)
        {
            double[,] deltaarray = { { 0.5 * (N[2, 1, 1] - N[0, 1, 1]), 0.5 * (N[1, 2, 1] - N[1, 0, 1]), 0.5 * (N[1, 1, 2] - N[1, 1, 0]) } };
            Matrix<double> delta = Matrix<double>.Build.DenseOfArray(deltaarray).Transpose();
            return delta;
        }

        // returns the estimated Hessian matrix of N
        private Matrix<double> Hessian(int[,,] N)
        {
            double dxx = N[0, 1, 1] - 2 * N[1, 1, 1] + N[2, 1, 1];
            double dyy = N[1, 0, 1] - 2 * N[1, 1, 1] + N[1, 2, 1];
            double dss = N[1, 1, 0] - 2 * N[1, 1, 1] + N[1, 1, 2];
            double dxy = 0.25 * (N[2, 2, 1] - N[0, 2, 1] - N[2, 0, 1] + N[0, 0, 1]);
            double dxs = 0.25 * (N[2, 1, 2] - N[0, 1, 2] - N[2, 1, 0] + N[0, 1, 0]);
            double dys = 0.25 * (N[1, 2, 2] - N[1, 0, 2] - N[1, 2, 0] + N[1, 0, 0]);
            double[,] Harray = { { dxx, dxy, dxs }, { dxy, dyy, dys }, { dxs, dys, dss } };
            Matrix<double> H = Matrix<double>.Build.DenseOfArray(Harray);
            return H;
        }

        // returns a list of dominant orientations for the key point k'
        private List<float> GetDominantOrientations(byte[][][,] G, Keypoint k)
        {
            double[] h = GetOrientationHistogram(G, k);
            SmoothCircular(h, n_smooth);
            List<float> A = FindPeakOrientations(h);
            return A;
        }

        // returns a new SIFT descriptor for the key point k'
        private SIFTdescriptor MakeSiftDescriptor(byte[][][,] G, Keypoint k,float θ)
        {
            byte[,] Gpq = GetScaleLevel(G, k.p, k.q);
            int m = Gpq.GetLength(0);
            int n = Gpq.GetLength(1);

            

            float scaleLevelq = (float)(sigma_0 * Math.Pow(2, (k.q/Q)));
            double descripterSize = s_Desc * scaleLevelq;

            double gWeightingFunctionWidth = 0.25 * descripterSize;
            int cutOffRadius = (int)(2.5 * gWeightingFunctionWidth);

            int uMin = Math.Max(k.x - cutOffRadius, 1);
            int uMax = Math.Min(k.x + cutOffRadius, m - 2);

            int vMin = Math.Max(k.y - cutOffRadius, 1);
            int vMax = Math.Min(k.y + cutOffRadius, n - 2);

            float[,,] gradientHistogram = new float[n_Spat, n_Spat, n_Angl]; 

            for (int u = uMin ; u <= uMax; u++)
            {
                for(int v = vMin ; v <= vMax; v++ )
                {
                    int radius2 = (u - k.x)*(u - k.x) + (v - k.y)*(v - k.y);
                    
                    if(radius2 < (cutOffRadius * cutOffRadius))
                    {
                        int uu = (int)(1.0 / descripterSize * (Math.Cos(-θ) * (u - k.x) - Math.Sin(-θ) * (v - k.y)));
                        var vv = (int)(1.0 / descripterSize * (Math.Sin(-θ) * (u - k.x) + Math.Cos(-θ) * (v - k.y)));

                        (double r, double phi) = GetGradientPolar(Gpq, u, v);
                        
                        double normalizedGradientAngle = (phi - θ) % (2 * Math.PI);
                        if(normalizedGradientAngle < 0) { normalizedGradientAngle += 2 * Math.PI; } //if the angle become negative
                        double wg = Math.Exp(-(radius2 / (2 * (gWeightingFunctionWidth * gWeightingFunctionWidth))));

                        float z = (float)(r*wg);
                        gradientHistogram = UpdateGradientHistogram(gradientHistogram, uu, vv, normalizedGradientAngle, z);

                    }
                }
            }

            byte[] fSift = MakeSiftFeatureVector(gradientHistogram);

            float sigma = (float)(sigma_0 * Math.Pow(2, k.p + k.q / Q));
            int xx = (int)(Math.Pow(2, k.p) * k.x);
            int yy = (int)(Math.Pow(2, k.p) * k.y);

            SIFTdescriptor descriptor = new SIFTdescriptor(xx, yy,sigma, θ,fSift);
            return descriptor;
        }

        public byte[] MakeSiftFeatureVector(float[,,] gradientHistogram)
        {
            float[] f = new float[n_Spat * n_Spat * n_Angl];
            int m = 0;

            for (int i = 0; i < n_Spat; i++)
            {
                for (int j = 0; j < n_Spat; j++)
                {
                    for (int k = 0; k < n_Angl; k++)
                    {
                        f[m] = gradientHistogram[i, j, k];
                        m++;
                    }

                }
            }

            f = Normalize(f);
            f = ClipPeaks(f, (float)t_Fclip);
            f = Normalize(f);

            byte[] fByte = MapToBytes(f, s_Fscale);

            return fByte;
        }

        public byte[] MapToBytes(float[] x, double s )
        {
            

            byte[] v = new byte[x.GetLength(0)];

            for (int i = 0; i < x.GetLength(0); i++)
            {
                double a = Math.Round(s*x[i]);
                v[i] = (byte)Math.Min(a, 255.0);
            }

            return v;
        }

        public float[] ClipPeaks(float[] x, float max)
        {
            int n = x.GetLength(0);

            for (int i = 0;i < n; i++)
            {
                x[i] = Math.Min(x[i], max);
            }

            return x;
        }
        public float[] Normalize(float[] array)
        {
            int n = array.GetLength(0);

            float sum = array.Sum();

            if (sum != 0)
            {
                for (int i = 0; i < n; i++)
                {

                    array[i] = array[i] / sum;

                }
            }
            return array;
        }

        public float[,,] UpdateGradientHistogram(float[,,] gradienHistogram, float u, float v,double phi,float z)
        {
            double i = n_Spat * u + 0.5 * (n_Spat - 1);
            double j = n_Spat * v + 0.5 * (n_Spat - 1);

            double k = n_Angl * phi/(2*Math.PI);

            int i0 = (int)i;
            int i1 = i0+1;
            int[] iArray = { i0, i1 };

            int j0 = (int)j;
            int j1 = j0+1;
            int[] jArray = { j0, j1 };


            int k0 = (int)k % n_Angl;
            int k1 = (k0 + 1) % n_Angl;
            int[] kArray = { k0, k1 };

            double a0 = i1 - i;
            double a1 = 1 - a0;
            double[] aArray = { a0, a1 };

            double b0 = j1 - j;
            double b1 = 1 - b0;
            double[] bArray = {b0, b1 };

            double y0 = (int)k + 1 - i;
            double y1 = 1 - y0;
            double[] yArray = {y0, y1};

            for(int a = 0 ; a <= 1; a++)
            {
                if (0 <= iArray[a] && iArray[a] < n_Spat)
                {
                    for(int b = 0 ; b <= 1; b++)
                    {
                        if (0 <= jArray[b] && jArray[b]<n_Spat)
                        {
                            for (int c = 0 ; c <= 1; c++)
                            {
                                gradienHistogram[iArray[a], jArray[b], kArray[c]] += (float)(z * aArray[a] * bArray[b] * yArray[c]);
                            }
                        }
                    }
                }
            }
            return gradienHistogram;

        }

        

        private void SmoothCircular(double[] x, int iter)
        {
            double[] h = { 0.25, 0.5, 0.25 };
            int n = x.Length;
            for (int i = 1; i == iter; i++)
            {
                double s = x[0];
                double p = x[n - 1];

                for (int j = 0; j == n - 2; j++)
                {
                    double c = x[j];
                    x[j] = (h[0] * p + h[1] * c + h[2] * x[j + 1]);
                    p = c;
                }
                x[n - 1] = h[0] * p + h[1] * x[n - 1] + h[2] * s;
            }

            return;
        }
        private List<float> FindPeakOrientations(double[] h)
        {
            int n = h.Length;
            double h_max = h.Max();
            List<float> A = new List<float>();

            for (int k = 1; k < n-1; k++)
            {
                double hc = h[k];
                if (hc > t_DomOr * h_max)
                {
                    double hp = h[k - 1] % n;
                    double hn = h[k + 1] % n;
                    if (hc > hp && hc > hn)
                    {
                        double k_new = k + (hp - hn) / (2 * (hp - (2 * hc) + hn));
                        double theta = (k_new * 2 * Math.PI / n) % (2 * Math.PI);
                        A.Add((float)theta);
                    }
                }
            }

            return A;
        }

        private double[] GetOrientationHistogram(byte[][][,] G, Keypoint k)
        {
            byte[,] Gpq = GetScaleLevel(G, k.p, k.q);
            int row = Gpq.GetLength(0);
            int col = Gpq.GetLength(1);
            double[] h = new double[n_Orient];
            
            double sigma_w = 1.5 * sigma_0 * Math.Pow(2, k.q / Q);
            double r_w = Math.Max(1, 2.5 * sigma_w);
           
            double umin = Math.Max(Math.Floor(k.x - r_w), 1);
            double umax = Math.Min(Math.Ceiling(k.x+r_w), row-2);
            double vmin = Math.Max(Math.Floor(k.y - r_w), 1);
            double vmax = Math.Min(Math.Ceiling(k.y + r_w), col-2);

            for (int u = (int)umin;  u <= (int)umax; u++)
                for (int v = (int)vmin; v <= (int)vmax; v++)
                {
                    double r_sqrt = Math.Pow((u - k.x), 2) + Math.Pow((v - k.y), 2);
                    if (r_sqrt < (r_w * r_w))
                    {
                        Tuple<double, double> Rphi = GetGradientPolar(Gpq, u, v);
                        double inside_wg = r_sqrt / (2 * Math.Pow(sigma_w, 2)) * -1; 
                        double w_g = Math.Exp(inside_wg);
                        double z = Rphi.Item1 * w_g;
                        double kphi = n_Orient * Rphi.Item2 / (2*Math.PI);
                        if (kphi < 0)
                            kphi += 360;
                        double alpha = kphi - Math.Floor(kphi);
                        int k0 = (int)Math.Floor(kphi)%n_Orient;
                        int k1 = (k0 + 1) % n_Orient;
                        h[k0] = h[k0] + ((1+ alpha)*z);
                        h[k1] = h[k1] + (alpha*z);
                    }    
                        
                }
            return h;

        }

        private Tuple<double, double> GetGradientPolar(byte[,] Gpq, int u, int v)
        {
            double dx = 0.5 * (Gpq[u+1, v] - Gpq[u - 1, v]);
            double dy = 0.5 * (Gpq[u, v+1] - Gpq[u, v-1]);

            double R = Math.Sqrt((dx * dx) + (dy * dy));
            double phi = Math.Atan2(dx, dy);

            return new Tuple<double, double>(R, phi);
        }
    }

    public class SIFTdescriptor
    {
        public int x;
        public int y;
        public double sigma;
        public double orientation;
        byte[] fsift;

        public SIFTdescriptor(int x, int y, double sigma, double orientation, byte[] fsift)
        {
            this.x = x;
            this.y = y;
            this.sigma = sigma;
            this.orientation = orientation;
            this.fsift = fsift;
        }
    }

    class Keypoint
    {
        public int p; //octave
        public int q; //scale level
        public int x; //spatial positon (x,y) (in octave's coördinates) 
        public int y;

        public Keypoint(int p, int q, int x, int y)
        {
            this.p = p;
            this.q = q;
            this.x = x;
            this.y = y;
        }

        public static Keypoint operator +(Keypoint a, Keypoint b)
        {
            return new Keypoint(a.p + b.p, a.q + b.q, a.x + b.x, a.y + b.y);
        }
    }
}
