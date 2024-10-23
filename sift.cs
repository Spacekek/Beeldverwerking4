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
