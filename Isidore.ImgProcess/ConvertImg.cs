﻿using System;
using System.Drawing;

namespace Isidore.ImgProcess
{
    /// <summary>
    /// Converts data to and from bitmaps and color arrays
    /// </summary>
    public class ConvertImg
    {
        /// <summary>
        /// Converts a 2D numeric array to a gray scale bitmap
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> 2D data array </param>
        /// <returns> gray scale bitmap </returns>
        public static Bitmap toBitmap<T>(T[,] arr)
        {
            int xLen = arr.GetLength(0);
            int yLen = arr.GetLength(1);
            Int64 Len = arr.LongLength;

            double maxVal = double.MinValue;
            double minVal = double.MaxValue;

            Type dType = typeof(double);
            for (Int64 idx = 0; idx < Len; idx++)
            {
                double pix = (double)Convert.ChangeType(
                    arr[idx % xLen, idx / xLen], dType);
                if (maxVal < pix)
                    maxVal = pix;
                if (minVal > pix)
                    minVal = pix;
            }
            maxVal -= minVal; // maximum = maxVal + minVal

            Bitmap img = new Bitmap(xLen, yLen);

            if (maxVal == 0.0) return img;

            for (int xIdx = 0; xIdx < xLen; xIdx++)
                for (int yIdx = 0; yIdx < yLen; yIdx++)
                {
                    int pix = (int)(255.0 * (Convert.ToDouble(
                        arr[xIdx, yIdx]) - minVal) / maxVal);
                    Color rgb = Color.FromArgb(pix, pix, pix);
                    img.SetPixel(xIdx, yIdx, rgb);
                }
            
            return img;
        }

        /// <summary>
        /// Converts a 2D color array into a numeric array where 
        /// each color is converted to gray scale
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="color"> 2D color array </param>
        /// <returns> numeric array </returns>
        public static T[,] toGrayScale<T>(Color[,] color)
        {
            int width = color.GetLength(0);
            int height = color.GetLength(1);
            T[,] gimg = new T[width, height];
            for (int k1 = 0; k1 < width; k1++)
                for (int k2 = 0; k2 < height; k2++)
                    gimg[k1, k2] = (T)Convert.ChangeType(color[k1, k2].R + 
                        color[k1, k2].B + color[k1, k2].G, typeof(T));
            return gimg;
        }

        /// <summary>
        /// Converts a bitmap to a 2D color array
        /// </summary>
        /// <param name="bitmap"> bitmap to convert </param>
        /// <returns> color array </returns>
        public static Color[,] toColor(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            Color[,] img = new Color[width, height];
            for (int k1 = 0; k1 < width; k1++)
                for (int k2 = 0; k2 < height; k2++)
                    img[k1, k2] = bitmap.GetPixel(k1, k2);
            return img;
        }
    }
}
