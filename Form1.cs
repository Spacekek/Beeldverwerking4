using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using System.Diagnostics;



namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;

        // main parameters for object detection

        public INFOIBV()
        {
            InitializeComponent();
        }

        /*
         * loadButton_Click: process when user clicks "Load" button
         */
        private void loadImageButton_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // open file dialog
            {
                string file = openImageDialog.FileName;                     // get the file name
                imageFileName.Text = file;                                  // show file name
                if (InputImage != null) InputImage.Dispose();               // reset image
                InputImage = new Bitmap(file);                              // create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // dimension check (may be removed or altered)
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                    pictureBox1.Image = (Image)InputImage;                 // display input image
            }
        }


        /*
         * applyButton_Click: process when user clicks "Apply" button
        */
        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)

            // copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)

            // execute image processing steps
            byte[,] workingImage = convertToGrayscale(Image);               // convert image to grayscale
            var existingFeatures = ReadFeatures("features.xml");
            workingImage = DetectObject(workingImage, existingFeatures);           // processing functions

            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }
            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }


        /*
         * saveButton_Click: process when user clicks "Save" button
         */
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // save the output image
        }


        /*
         * convertToGrayScale: convert a three-channel color image to a single channel grayscale image
         * input:   inputImage          three-channel (Color) image
         * output:                      single-channel (byte) image
         */
        private byte[,] convertToGrayscale(Color[,] inputImage)
        {
            // create temporary grayscale image of the same size as input, with a single channel
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // process all pixels in the image
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
            {
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                {
                    Color pixelColor = inputImage[x, y];                    // get pixel color
                    byte average = (byte)((pixelColor.R + pixelColor.B + pixelColor.G) / 3); // calculate average over the three channels
                    tempImage[x, y] = average;                              // set the new pixel color at coordinate (x,y)
                }
                progressBar.PerformStep();                              // increment progress bar
            }

            progressBar.Visible = false;                                    // hide progress bar

            return tempImage;
        }

        /*
         * DetectObject: Detects object described in the existingFeatures and draws a rectangle around it
         * input:   inputImage          single-channel (Color) image
         *          existingFeatures    List of SIFTdescriptors describing an object
         * output:                      single-channel (byte) image
         */
        private byte[,] DetectObject(byte[,] inputImage, List<SIFTdescriptor> existingFeatures)
        {
            // preprocessing for now only histogram equalization
            byte[,] image = PreProcessImage(inputImage);

            Sift sifter = new Sift();
            var features = sifter.GetSiftFeatures(image);

            double detectDistance = 200;
            try
            {
                detectDistance = Convert.ToDouble(textBox1.Text);
            }
            catch
            {
                textBox1.Text = Math.Round(detectDistance).ToString();
            }

            List<SIFTdescriptor> matches = MatchFeatures(existingFeatures, features, detectDistance);

            double percentageFound = (double)matches.Count / (double)existingFeatures.Count;
            label6.Text = matches.Count.ToString();
            label7.Text = existingFeatures.Count.ToString();
            label5.Text = Math.Round(percentageFound * 100, 2).ToString();

            double percentageNeeded = 0.5;
            try
            {
                percentageNeeded = Convert.ToDouble(textBox3.Text) / 100;
            }
            catch 
            { 
                textBox3.Text = Math.Round(percentageNeeded*100, 2).ToString();
            }
            

            if (percentageFound < percentageNeeded)
                return inputImage; // don't draw anything

            // draw rectangle around detected object
            int xmin = int.MaxValue;
            int xmax = int.MinValue;
            int ymin = int.MaxValue;
            int ymax = int.MinValue;

            foreach (var feature in matches)
            {
                // find min and max
                if (feature.x < xmin)
                    xmin = feature.x;
                if (feature.y < ymin)
                    ymin = feature.y;
                if (feature.y > ymax)
                    ymax = feature.y;
                if (feature.x > xmax)
                    xmax = feature.x;
            }

            Point min = new Point(xmin, ymin); // minimum x and y coordinates of features
            Point max = new Point(xmax, ymax); // maximum x and y coordinates of features

            byte[,] imageWithRectangle = ImageOperations.drawRectangle(inputImage, min, max);

            return imageWithRectangle;
        }

        private List<SIFTdescriptor> CreateExistingFeatures(List<byte[,]> images)
        {
            Sift sift = new Sift();
            byte[,] firstimage = PreProcessImage(images[0]);
            List<SIFTdescriptor> matches = sift.GetSiftFeatures(firstimage);
            for (int i = 1; i < images.Count; i++)
            {
                byte[,] image = PreProcessImage(images[i]);
                Sift sifter = new Sift();
                var features = sifter.GetSiftFeatures(image);
                double existingDistance = 200;
                try
                {
                    existingDistance = Convert.ToDouble(textBox2.Text);
                }
                catch
                {
                    textBox2.Text = Math.Round(existingDistance).ToString();
                }
                matches = MatchFeatures(matches, features, existingDistance);
            }
            return matches;
        }

        // applies several preprocessing effects on the image to make the object detection more accurate
        private byte[,] PreProcessImage(byte[,] inputImage)
        {
            byte[,] outImage = ImageOperations.adjustContrast(inputImage); // adjust contrast so we use full contrast space
            outImage = ImageOperations.histogramEqualization(outImage); //for more detail to work with use histogram equalization
            outImage = ImageOperations.medianFilter(outImage, 3); // reduce noise with median filter
            return outImage;
        }

        // saves a list of features to a file
        private void SaveFeatures(String filePath, List<SIFTdescriptor> features)
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(List<SIFTdescriptor>));
                writer = new StreamWriter(filePath, false);
                serializer.Serialize(writer, features);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        // reads a list of features from a file
        private List<SIFTdescriptor> ReadFeatures(string filePath)
        {
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(List<SIFTdescriptor>));
                reader = new StreamReader(filePath);
                return (List<SIFTdescriptor>)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        // matches descriptors between 2 lists of descriptors when features are within a certain distance.
        private List<SIFTdescriptor> MatchFeatures(List<SIFTdescriptor> existingFeatures, List<SIFTdescriptor> newFeatures, double maxDist)
        {
            var matches = new ConcurrentBag<SIFTdescriptor>();
            var times = new ConcurrentBag<double>();

            Parallel.ForEach(existingFeatures, feature =>
            {
                SIFTdescriptor match = null;
                double dist = double.MaxValue;

                foreach (var feature2 in newFeatures)
                {
                    double d = FeatureDistance(feature, feature2);
                    if (d < dist)
                    {
                        dist = d;
                        match = feature2;
                    }
                }

                if (match != null && dist < maxDist)
                {
                    matches.Add(match);
                }
            });

            return matches.ToList();
        }

        // calculates the distance between 2 SIFTdescriptors (simple euclidian distance between feature vectors)
        private double FeatureDistance(SIFTdescriptor A, SIFTdescriptor B)
        {
            double dist = 0;
            for (int i = 0; i < A.fsift.Length; i++)
            {
                dist += Math.Pow(A.fsift[i] - B.fsift[i], 2);
            }
            return Math.Sqrt(dist);
        }
        
        // button for creating and saving existing features
        private void button1_Click(object sender, EventArgs e)
        {
            List<byte[,]> inputImages = new List<byte[,]>();
            openImageDialog.Multiselect = true;

            if (openImageDialog.ShowDialog() == DialogResult.OK)             // open file dialog
            {
                foreach (string file in openImageDialog.FileNames)           // get the file names
                {
                    InputImage = new Bitmap(file);                              // create new Bitmap from file
                    if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // dimension check (may be removed or altered)
                        MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                    else
                    {
                        Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)

                        // copy input Bitmap to array            
                        for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                            for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                                Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)

                        byte[,] trainimage = convertToGrayscale(Image);
                        inputImages.Add(trainimage);
                    }
                }       
            }

            List<SIFTdescriptor> features = CreateExistingFeatures(inputImages);
            SaveFeatures("features.xml", features);
        }
    }
}