using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOIBV
{
    internal class Sift
    {
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

        }

        // GetDominantOrientations
        // input:
        // G, hierarchical Gaussian scale space
        // k', refined key point at octave p, scale lever q and spatial position x,y

        // returns a list of dominant orientations for the key point k'
        private void GetDominantOrientations(List<int> G, Keypoint k)
        {
            histogram h = GetOrientationHistogram(G, k);
            SmoothCircular(h, n);
            List<int> A = FindPeakOrientations(h); //list of dominant orientations 
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
        
        private void SmoothCircular(x, iter)
        {

            return;
        }
        private void FindPeakOrientations(float[] h)
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

        }
        private void GetOrientationHistogram(List<int> G, Keypoint k)
        {
            Gpq = GetScaleLevel(G, k.p, k.q);
            int M, N = size(Gpc);

        }
    }

    class Keypoint
    {
        public int p; //octave
        public int q; //scale level
        public int x; //spatial positon (x,y) (in octave's coördinates)
        public int y;
    }
}
