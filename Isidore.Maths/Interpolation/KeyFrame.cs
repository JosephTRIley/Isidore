﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Isidore.Maths
{
    /// <summary>
    /// KeyFrame is used to interpolate scalar data over time
    /// </summary>
    /// <typeparam name="T"> Data type </typeparam>
    public class KeyFrame<T>
    {
        # region Fields & Properties
        
        /// <summary>
        /// Animates transform (true) or locks to the first transform 
        /// (false)
        /// </summary>
        public bool Animate = true;

        /// <summary>
        /// Interpolation method.  Options: "linear" = linear transformation
        /// </summary>
        public string Interpolation = "linear";

        /// <summary>
        /// Transform matrix array
        /// </summary>
        protected T[] values;

        /// <summary>
        /// Time line array matched to trans transform array
        /// </summary>
        protected double[] times;

        /// <summary>
        /// Either updates the current time of the path 
        /// (via InterpolateToTime) or returns the current set time
        /// </summary>
        public double CurrentTime
        {
            get { return currentTime; }
        }
        /// <summary>
        /// Instance current time (Saves unnecessary interpolations)
        /// </summary>
        protected double currentTime;

        /// <summary>
        /// Returns the interpolated transform for the current time
        /// </summary>
        public T CurrentValue
        {
            get
            {
                if (Animate)
                    return currentValue;
                else
                    return values[0];
            }
        }
        /// <summary>
        /// Instance current transform matrix 
        /// (Saves unnecessary interpolations)
        /// </summary>
        protected T currentValue;

        /// <summary>
        /// Data type
        /// </summary>
        protected Type type;

        /// <summary>
        /// All values in the key frame animation
        /// </summary>
        public T[] Values { get { return values; } }

        /// <summary>
        /// All time stamps corresponding to each value in Values
        /// </summary>
        public double[] Times { get { return times; } }

        # endregion Fields & Properties
        # region Constructors

        /// <summary>
        /// KeyFrame Constructor 
        /// </summary>
        /// <param name="valuesArray"> Value array </param>
        /// <param name="timeStamps"> Matching time stamps </param>
        /// <param name="interpolation"> Interpolation method: 
        /// "linear </param>
        public KeyFrame(T[] valuesArray = null, double[] timeStamps = null,
            string interpolation = "linear")
        {
            type = typeof(T);
            times = timeStamps ?? new double[] { 0.0 };
            values = valuesArray ?? new T[1]; // Makes a new one if null;
            Interpolation = interpolation;

            // This forces a first call in classes
            currentTime = Double.NaN;
        }

        /// <summary>
        /// Constructs a KeyFrame with one value, effectively 
        /// making it a static value
        /// </summary>
        /// <param name="value"> Value </param>
        /// <param name="time"> Time </param>
        public KeyFrame(T value, double time) : 
            this(new T[] { value }, new double[] { time }) { }

        # endregion Constructor
        # region Methods

        /// <summary>
        /// Adds the transform to the animation array at the 
        /// appropriate place in the time line
        /// </summary>
        /// <param name="newValue"> Transformation </param>
        /// <param name="timeStamp"> Time associated with this 
        /// transformation </param>
        public void AddKeys(T newValue, double timeStamp)
        {
            // Casts to a list
            List<T> lVals = values.ToList();
            List<double> lTimes = times.ToList();

            // If the time is greater than the last time line, then adds on
            if (timeStamp > times.GetLowerBound(0))
            {
                lVals.Add(newValue);
                lTimes.Add(timeStamp);
            }
            else // Finds insert index
            {
                int idx = 0;
                if (this.values.Length > 0)
                    while (timeStamp > times[idx])
                        idx++;
                // if the time already exists, replaces
                if (timeStamp == times[idx])
                {
                    lVals[idx] = newValue;
                    lTimes[idx] = timeStamp;
                }
                else // otherwise, inserts
                {
                    lVals.Insert(idx, newValue);
                    lTimes.Insert(idx, timeStamp);
                }
            }
            // Cast back to an array
            values = lVals.ToArray();
            times = lTimes.ToArray();
        }

        /// <summary>
        /// Removes the value and time at index "idx"
        /// </summary>
        /// <param name="idx"> Index of transform to remove </param>
        public void RemoveKeys(int idx)
        {
            List<T> lVals = values.ToList();
            List<double> lTimes = times.ToList();
            lVals.RemoveAt(idx);
            lTimes.RemoveAt(idx);
            values = lVals.ToArray();
            times = lTimes.ToArray();
        }

        /// <summary>
        /// Merges two animation transforms by interpolating to unique 
        /// time points and applying the new transform to the existing 
        /// transform
        /// </summary>
        /// <param name="kf2"> Second key frame to merge with this 
        /// one </param>
        public void MergeKeys(KeyFrame<T> kf2)
        {
            double thisCurrTime = currentTime;

            List<double> timePts = times.ToList();

            for (int idx = 0; idx < kf2.times.Length; idx++)
                if (!timePts.Exists(x => x == kf2.times[idx]))
                    timePts.Add(kf2.times[idx]);
            timePts.Sort();
            
            T[] mVals = new T[timePts.Count];

            for(int idx=0; idx<mVals.Length; idx++)
            {
                //T tVals = kf2.InterpolateToTime(timePts[idx]);
                //InterpolateToTime(timePts[idx]);
                mVals[idx] = Maths.Operator.Multiply
                    (kf2.InterpolateToTime(timePts[idx]),
                    InterpolateToTime(timePts[idx]));       
            }

            times = timePts.ToArray();
            values = mVals;
            InterpolateToTime(thisCurrTime);
        }

        /// <summary>
        /// Scales all key values
        /// </summary>
        /// <param name="scalar"> scalar value to multiply </param>
        public void Scale(T scalar)
        {
            Operator.Multiply(scalar, values);
        }

        /// <summary>
        /// Offsets all key values
        /// </summary>
        /// <param name="scalar"> scalar value to add </param>
        public void Offset(T scalar)
        {
            Operator.Add(scalar, values);
        }

        /// <summary>
        /// Interpolates between time steps.  If the time point is 
        /// outside the time line, the closest transform is returned
        /// </summary>
        /// <param name="timePt"> time point to interpolate to </param>
        /// <returns> Interpolated transform </returns>
        public T InterpolateToTime(double timePt)
        {
            // Avoid redundancy by saving the current time's transform
            if (timePt == currentTime)
                return currentValue;
            currentTime = timePt;

            // Outside bounds handler
            if (timePt <= times[0] || !Animate)
                currentValue = values[0];
            else if (timePt >= times.Last())
                currentValue = values.Last();
            else
            {
                // Finds closest matches
                int idx = 0;
                do {} while (times[++idx] <= timePt);
                idx--;

                // If on-point, don't interpolate
                if (timePt == times[idx])
                    currentValue = values[idx];
                else // Linear interpolation
                {
                    // Interpolation factor
                    double fac = (timePt - times[idx]) /
                         (times[idx + 1] - times[idx]);

                    // Calls linear interpolation
                    if (typeof(T) == typeof(Point))
                    {
                        var tempVal = Maths.Interpolate.Linear(
                            values[idx] as Point, values[idx + 1] as Point,
                            fac);
                        currentValue = Operator.Convert<Point, T>(tempVal);
                    }
                    else
                        currentValue = Maths.Interpolate.Linear(values[idx],
                        values[idx + 1], fac);
                }
            }
            return currentValue;
        }

        /// <summary>
        /// Returns a clone of this KeyFramePath by performing a deep copy
        /// </summary>
        /// <returns> KeyFramePath clone </returns>
        public KeyFrame<T> Clone()
        {
            KeyFrame<T> newPath = new KeyFrame<T>((T[])values.Clone(), 
                (double[])times.Clone(), Interpolation);
            newPath.Animate = Animate;
            return newPath;
        }

        # endregion Methods
    }
}
