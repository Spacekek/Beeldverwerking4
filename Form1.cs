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



namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;

        /*
         * this enum defines the processing functions that will be shown in the dropdown (a.k.a. combobox)
         * you can expand it by adding new entries to applyProcessingFunction()
         */
        private enum ProcessingFunctions
        {
            loadGreyImage,
            AdjustContrast,
            ConvolutionFilter,
            MedianFilter,
            DetectEdges,
            HistogramEqualization,
            createSIFTscaleSpace,
            GetSiftFeatures,
            DetectObject
        }

        /*
         * these are the parameters for your processing functions, you should add more as you see fit
         * it is useful to set them based on controls such as sliders, which you can add to the form
         */
        private byte filterSize = 11;
        private float filterSigma = 1f;
        private byte threshold = 127;


        public INFOIBV()
        {
            InitializeComponent();
            populateCombobox();
            populate_sliders_labels(); //populates sliders with min/max values and current value + populate lable with value

        }
        
        /*
         * populateCombobox: populates the combobox with items as defined by the ProcessingFunctions enum
         */
        private void populateCombobox()
        {
            foreach (string itemName in Enum.GetNames(typeof(ProcessingFunctions)))
            {
                string ItemNameSpaces = Regex.Replace(Regex.Replace(itemName, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
                comboBox.Items.Add(ItemNameSpaces);
            }
            comboBox.SelectedIndex = 0;
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
            workingImage = applyProcessingFunction(workingImage);           // processing functions

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
         * applyProcessingFunction: defines behavior of function calls when "Apply" is pressed
         */
        private byte[,] applyProcessingFunction(byte[,] workingImage)
        {
            sbyte[,] horizontalKernel = {
                                                    {-1,0,1 },
                                                    {-2,0,2},
                                                    {-1,0,1},
                                                 };                      // Define this kernel yourself
            sbyte[,] verticalKernel = {
                                                    { -1, -2, -1 },
                                                    { 0, 0, 0},
                                                    { 1, 2, 1}
                                              };                         // Define this kernel yourself
            switch ((ProcessingFunctions)comboBox.SelectedIndex)
            {
                case ProcessingFunctions.AdjustContrast:
                    return ImageOperations.adjustContrast(workingImage);
                case ProcessingFunctions.ConvolutionFilter:
                    float[,] filter = ImageOperations.createGaussianFilter(filterSize, filterSigma);
                    return ImageOperations.convolveImage(workingImage, filter);
                case ProcessingFunctions.MedianFilter:
                    return ImageOperations.medianFilter(workingImage, filterSize);
                case ProcessingFunctions.DetectEdges:
                    return ImageOperations.edgeMagnitude(workingImage, horizontalKernel, verticalKernel);
                case ProcessingFunctions.loadGreyImage:
                    return workingImage;
                case ProcessingFunctions.HistogramEqualization:
                    return ImageOperations.histogramEqualization(workingImage);
                case ProcessingFunctions.createSIFTscaleSpace:
                    Sift sifter = new Sift();
                    (byte[][][,] G, int[][][,] D) = sifter.BuildSiftScaleSpace(workingImage, (float)0.5, (float)1.6, 4, 3);
                    return workingImage;
                case ProcessingFunctions.GetSiftFeatures:
                    Sift sifter2 = new Sift();
                    sifter2.GetSiftFeatures(workingImage);
                    return workingImage;
                case ProcessingFunctions.DetectObject:
                    var existingFeatures = new List<SIFTdescriptor>(); // load in existing features
                    return DetectObject(workingImage, existingFeatures);
                default:
                    return null;
            }
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

        public void populate_sliders_labels() // populates the sliders with values
        {
            label1.Text = "Threshold is: " + threshold.ToString();
            label2.Text = "Filter sigma is:" + filterSigma.ToString();
            label3.Text = "Filter size is: " + filterSize.ToString();
            label4.Text = "structure size is: " + structure.Value.ToString();

        }
        public byte[] oneven_waardes = { 1, 3, 5, 7, 9, 11,13 };
        public void trackBar1_Scroll(object sender, EventArgs e) //trackbar threshold
        {
            trackBar1 = sender as TrackBar;
 
            threshold = (byte)trackBar1.Value;
            label1.Text = "Threshold is: " + threshold.ToString();
        }

        public void label1_Click(object sender, EventArgs e)
        {
            label1 = sender as Label;
            label1.Text = threshold.ToString();
        }

        private void trackBar2_Scroll(object sender, EventArgs e) //trackbar filter sigma
        {
            trackBar2 = sender as TrackBar;
            filterSigma = trackBar2.Value;
            label2.Text = "Filter sigma is: " + filterSigma.ToString();

        }

        private void trackBar3_Scroll(object sender, EventArgs e) // trackbar filter size
        {
            trackBar3 = sender as TrackBar;
            filterSize = oneven_waardes[trackBar3.Value];
            label3.Text = "Filter size is: " + oneven_waardes[trackBar3.Value];
        }
        // assumes grayscale input
        private byte[,] DetectObject(byte[,] inputImage, List<SIFTdescriptor> existingFeatures)
        {
            // preprocessing for now only histogram equalization
            byte[,] equalized = ImageOperations.histogramEqualization(inputImage);

            Sift sifter = new Sift();
            var features = sifter.GetSiftFeatures(equalized);

            List<SIFTdescriptor> matches = new List<SIFTdescriptor>();

            // match features with existing features
            foreach (var feature in features)
            {
                if (existingFeatures.Contains(feature))
                    matches.Add(feature);
            }

            // 50% of features needed
            double percentageNeeded = 0.5;

            if (matches.Count / existingFeatures.Count < percentageNeeded)
                return inputImage; // don't draw anything

            // draw rectangle around detected object
            Point min = new Point(); // minimum x and y coordinates of features
            Point max = new Point(); // maximum x and y coordinates of features
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
            byte[,] imageWithRectangle = ImageOperations.drawRectangle(inputImage, min, max);

            return imageWithRectangle;
        }

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
        }
    }
}