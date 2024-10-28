using System;
using System.Threading.Tasks;

namespace INFOIBV
{
    public static class ImageOperations
    {

        // helper function to apply arbitrary function to each pixel in the image
        public static byte[,] applyFunction(byte[,] inputImage, Func<byte, byte> function)
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
        public static byte[,] adjustContrast(byte[,] inputImage)
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
        public static float[,] createGaussianFilter(byte size, float sigma)
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
        public static byte[,] convolveImage(byte[,] inputImage, float[,] filter)
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
        public static byte[,] medianFilter(byte[,] inputImage, byte size)
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
        public static byte[,] edgeMagnitude(byte[,] inputImage, sbyte[,] K_horizontal, sbyte[,] K_vertical)
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
        public static byte[,] histogramEqualization(byte[,] inputImage)
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
    }
}
