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
using System.Windows.Forms.DataVisualization.Charting;



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
            createSIFTscaleSpace

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
                    return adjustContrast(workingImage);
                case ProcessingFunctions.ConvolutionFilter:
                    float[,] filter = createGaussianFilter(filterSize, filterSigma);
                    return convolveImage(workingImage, filter);
                case ProcessingFunctions.MedianFilter:
                    return medianFilter(workingImage, filterSize);
                case ProcessingFunctions.DetectEdges:
                    return edgeMagnitude(workingImage, horizontalKernel, verticalKernel);
                case ProcessingFunctions.loadGreyImage:
                    return workingImage;
                case ProcessingFunctions.HistogramEqualization:
                    return histogramEqualization(workingImage);
                case ProcessingFunctions.createSIFTscaleSpace:
                    BuildSiftScaleSpace(workingImage, (float)0.5, (float)1.6, 4, 3);
                    return workingImage;
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

        // helper function to apply arbitrary function to each pixel in the image
        private byte[,] applyFunction(byte[,] inputImage, Func<byte, byte> function)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            Parallel.For(0, inputImage.GetLength(0), x =>               // loop over columns
            {
                Parallel.For(0, inputImage.GetLength(1), y =>            // parallel loop over rows
                {
                    tempImage[x, y] = function(inputImage[x, y]);        // apply function to pixel
                });
            });
            return tempImage;
        }

        /*
         * adjustContrast: create an image with the full range of intensity values used
         * input:   inputImage          single-channel (byte) image
         * output:                      single-channel (byte) image
         */
        private byte[,] adjustContrast(byte[,] inputImage)
        {
            // find min and max pixel values
            byte min = 255;
            byte max = 0;
            Parallel.For(0, inputImage.GetLength(0), x =>               // loop over columns
            {
                Parallel.For(0, inputImage.GetLength(1), y =>            // parallel loop over rows
                {
                    byte pixel = inputImage[x, y];
                    if (pixel < min) min = pixel;
                    if (pixel > max) max = pixel;
                });
            });

            // apply contrast stretching
            return applyFunction(inputImage, (byte pixel) => (byte)(255 * (pixel - min) / (max - min)));
        }


        /*
         * createGaussianFilter: create a Gaussian filter of specific square size and with a specified sigma
         * input:   size                length and width of the Gaussian filter (only odd sizes)
         *          sigma               standard deviation of the Gaussian distribution
         * output:                      Gaussian filter
         */
        private float[,] createGaussianFilter(byte size, float sigma)
        {
            // create temporary grayscale image
            float[,] filter = new float[size, size];

            // TODO: add your functionality and checks
            if (size % 2 == 0) throw new ArgumentException("Size must be odd");

            Parallel.For(0, size, x =>
            {
                Parallel.For(0, size, y =>
                {
                    // number is based on the formula for a 2D Gaussian distribution
                    filter[x, y] = (float)(1 / (2 * Math.PI * sigma * sigma) * Math.Exp(-(((x - size / 2) * (x - size / 2) + (y - size / 2) * (y - size / 2)) / (2 * sigma * sigma))));
                });
            });
            Console.WriteLine("Filter: " + filter);
            return filter;
        }


        /*
         * convolveImage: apply linear filtering of an input image
         * input:   inputImage          single-channel (byte) image
         *          filter              linear kernel
         * output:                      single-channel (byte) image
         */
        private byte[,] convolveImage(byte[,] inputImage, float[,] filter)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // TODO: add your functionality and checks, think about border handling and type conversion
            // border is last pixel value for now

            // add padding to image
            int padding = filter.GetLength(0) / 2;
            byte[,] paddedImage = new byte[inputImage.GetLength(0) + 2 * padding, inputImage.GetLength(1) + 2 * padding];
            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    paddedImage[x + padding, y + padding] = inputImage[x, y];
                }
            }
            // fill padding with border value
            for (int x = 0; x < padding; x++)
            {
                for (int y = 0; y < paddedImage.GetLength(1); y++)
                {
                    paddedImage[x, y] = inputImage[0, 0];
                    paddedImage[paddedImage.GetLength(0) - 1 - x, y] = inputImage[inputImage.GetLength(0) - 1, inputImage.GetLength(1) - 1];
                }
            }
            for (int y = 0; y < padding; y++)
            {
                for (int x = 0; x < paddedImage.GetLength(0); x++)
                {
                    paddedImage[x, y] = inputImage[0, 0];
                    paddedImage[x, paddedImage.GetLength(1) - 1 - y] = inputImage[inputImage.GetLength(0) - 1, inputImage.GetLength(1) - 1];
                }
            }

            // apply filter
            Parallel.For(padding, inputImage.GetLength(0) + padding, x =>
            {
                for (int y = padding; y < inputImage.GetLength(1) + padding; y++)
                {
                    float sum = 0;
                    for (int i = 0; i < filter.GetLength(0); i++)
                    {
                        for (int j = 0; j < filter.GetLength(1); j++)
                        {
                            sum += filter[i, j] * paddedImage[x - padding + i, y - padding + j];
                        }
                    }
                    tempImage[x - padding, y - padding] = (byte)sum;
                }
            });

            return tempImage;
        }


        /*
         * medianFilter: apply median filtering on an input image with a kernel of specified size
         * input:   inputImage          single-channel (byte) image
         *          size                length/width of the median filter kernel
         * output:                      single-channel (byte) image
         */
        private byte[,] medianFilter(byte[,] inputImage, byte size)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // TODO: add your functionality and checks, think about border handling and type conversion
            // border is handled by simply ignoring the pixels outside the image

            Parallel.For(0, inputImage.GetLength(0), x =>
            {
                Parallel.For(0, inputImage.GetLength(1), y =>
                {
                    byte[] values = new byte[size * size];
                    int index = 0;
                    for (int i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            int xIndex = x - size / 2 + i;
                            int yIndex = y - size / 2 + j;
                            if (xIndex >= 0 && xIndex < inputImage.GetLength(0) && yIndex >= 0 && yIndex < inputImage.GetLength(1))
                            {
                                values[index] = inputImage[xIndex, yIndex];
                                index++;
                            }
                        }
                    }
                    // shorten array to length index
                    byte[] shortenedValues = new byte[index];
                    Array.Copy(values, shortenedValues, index);
                    Array.Sort(shortenedValues);
                    // if the array is even, take the average of the two middle values
                    if (index % 2 == 0)
                    {
                        tempImage[x, y] = (byte)((shortenedValues[index / 2] + shortenedValues[index / 2 - 1]) / 2);
                    }
                    else
                        tempImage[x, y] = shortenedValues[index / 2];
                });
            });
            return tempImage;
        }


        /*
         * edgeMagnitude: calculate the image derivative of an input image and a provided edge kernel
         * input:   inputImage          single-channel (byte) image
         *          horizontalKernel    horizontal edge kernel
         *          virticalKernel      vertical edge kernel
         * output:                      single-channel (byte) image
         */
        private byte[,] edgeMagnitude(byte[,] inputImage, sbyte[,] K_horizontal, sbyte[,] K_vertical)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // TODO: add your functionality and checks, think about border handling and type conversion (negative values!)


            // gekozen voor randen te negeren
            int min = int.MaxValue;
            int max = int.MinValue;
            int[,] int_image = new int[inputImage.GetLength(0), inputImage.GetLength(1)];
            Console.WriteLine(min + max);
            for (int x = 1; x < inputImage.GetLength(0) - 2; x++)
            {
                for (int y = 1; y < (inputImage.GetLength(1) - 2); y++)
                {


                    int partial_gradiant_x = 0;
                    int partial_gradiant_y = 0;

                    for (int i = 0; i < K_horizontal.GetLength(0); i++)
                    {
                        for (int j = 0; j < K_vertical.GetLength(0); j++)
                        {
                            partial_gradiant_x = partial_gradiant_x + (inputImage[x + i, y + j] * K_horizontal[i, j]);
                            partial_gradiant_y = partial_gradiant_y + (inputImage[x + i, y + j] * K_vertical[i, j]);
                        }
                    }

                    double magnitude = Math.Sqrt((partial_gradiant_x * partial_gradiant_x) + (partial_gradiant_y * partial_gradiant_y));



                    if (magnitude != 0)
                    {
                        if (magnitude < min) { min = (int)magnitude; }
                        if (magnitude > max) { max = (int)magnitude; }
                    }



                    int_image[x, y] = (int)magnitude;



                }

                for (int i = 0; i < int_image.GetLength(0); i++)              // loop over afbeelding en normalize elke pixel
                {
                    for (int j = 0; j < int_image.GetLength(1); j++)
                    {
                        tempImage[i, j] = (byte)(((int_image[i, j] - min) * 255) / (max - min));
                    }
                }
            };


            return tempImage;
        }

        // histogram equalization
        private byte[,] histogramEqualization(byte[,] inputImage)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // create histogram
            int[] histogram = new int[256];
            for (int x = 0; x < inputImage.GetLength(0); x++)                 // loop over columns
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)            // loop over rows
                {
                    histogram[inputImage[x, y]]++;
                }
            }

            // calculate cumulative distribution function
            int[] cdf = new int[256];
            cdf[0] = histogram[0];
            for (int i = 1; i < 256; i++)
            {
                cdf[i] = cdf[i - 1] + histogram[i];
            }

            // normalize cdf
            for (int i = 0; i < 256; i++)
            {
                cdf[i] = (int)(255 * cdf[i] / (inputImage.GetLength(0) * inputImage.GetLength(1)));
            }

            // apply cdf to image
            Parallel.For(0, inputImage.GetLength(0) - 1, x =>               // loop over columns
            {
                Parallel.For(0, inputImage.GetLength(1) - 1, y =>            // parallel loop over rows
                {
                    tempImage[x, y] = (byte)cdf[inputImage[x, y]];
                });
            });

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

        ////
        /// ASSIGMNENT 4
        ///

        //
        private (byte[][][,], int[][][,]) BuildSiftScaleSpace(byte[,] InputImage, float sigmaS, float σ0, float P, float Q)
        {
            double initial_sigma = σ0 * Math.Pow(2, (-1.0 / Q));
            float initial_increment_sigma = (float)Math.Sqrt((initial_sigma * initial_sigma) - (sigmaS * sigmaS));

            //filter size moet vgm mee groeien. miss kijken of int kunnen maken 
            byte filtersize = (byte)(6 * initial_increment_sigma + 1);
            if (filtersize % 2 == 0) { filtersize += 1; } //zorgt dat filtersize altijd oneven is

            float[,] gausianFilter = createGaussianFilter(filtersize, initial_increment_sigma);  //maakt the gaussian filter

            byte[,] gausianFilteredImage = convolveImage(InputImage, gausianFilter); //past gaussian filter toe op Inputimage

            byte[][,] firstOctave = new byte[(int)Q][,];
            firstOctave = MakeGaussianOctave(gausianFilteredImage, Q, σ0); //maakt eerste octave van Q lang die gaussianfilter toepast op image met verschillende a 


            byte[][][,] octaves = new byte[(int)P][][,];
            octaves[0] = firstOctave;



            for (int p = 1; p < P; p++)
            {
                octaves[p] = MakeGaussianOctave(Decimate(octaves[p - 1][(int)Q - 1]), Q, σ0);
            }

            int[][][,] DOGoctaves = new int[(int)P][][,];

            for (int j = 0; j < P; j++)
            {
                DOGoctaves[j] = MakeDogOctave(octaves[j]);
            }

            return (octaves, DOGoctaves);
        }

        private byte[][,] MakeGaussianOctave(byte[,] gaussianFilteredImage, float Q, float σ0)
        {
            byte[][,] octave = new byte[(int)Q][,];
            float[,] gausianFilter;

            for (float i = 0; i < Q; i++)
            {
                float sd = (float)(σ0 * Math.Sqrt(Math.Pow(2.0, (2.0 * i / Q)) - 1));

                byte filtersize = (byte)(6 * sd + 1); //past filter size aan opbasis van sd
                if (filtersize % 2 == 0) { filtersize += 1; } //zorgt dat filtersize altijd oneven is
                if (sd == 0) { octave[(int)i] = gaussianFilteredImage; }
                else
                {
                    gausianFilter = createGaussianFilter(filtersize, sd);
                    octave[(int)i] = convolveImage(gaussianFilteredImage, gausianFilter);
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
    }
}