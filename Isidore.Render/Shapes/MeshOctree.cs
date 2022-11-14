﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represents an axis-aligned octree.  Since this is an octree,
    /// there are no spatial transforms.  
    /// </summary>
    public class MeshOctree: ICloneable
    {
        #region Fields & Properties

        /// <summary>
        /// Component mesh octboxes
        /// </summary>
        protected internal List<MeshOctBox> meshoctboxes;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor using a Mesh.  maxFacetCount sets the threshold
        /// for when to trigger a subdivision.  maxTreeDepth supersedes
        /// maxFacetCount.
        /// </summary>
        /// <param name="mesh"> Mesh to apply the octree to </param>
        /// <param name="maxFacetCount"> Maximum number of facets per 
        /// octbox </param>
        /// <param name="maxTreeDepth"> Maximum tree depth </param>
        public MeshOctree(Mesh mesh, int maxFacetCount = 20,
            int maxTreeDepth = 4):this(mesh.GlobalVertices, mesh.Facets,
                maxFacetCount, maxTreeDepth)
        { }

        /// <summary>
        /// Constructor using Mesh components.  maxFacetCount sets the 
        /// threshold for when to trigger a subdivision.  maxTreeDepth 
        /// supersedes maxFacetCount.
        /// </summary>
        /// <param name="vertices"> Mesh vertices list </param>
        /// <param name="facets"> Mesh facet list </param>
        /// <param name="maxFacetCount"> Maximum number of facets per 
        /// octbox </param>
        /// <param name="maxTreeDepth"> Maximum tree depth </param>
        public MeshOctree(Vertices vertices, List<int[]> facets, 
            int maxFacetCount = 25, int maxTreeDepth = 4)
        {
            // First octbox has and ID of 0
            meshoctboxes = new List<MeshOctBox>();
            meshoctboxes.Add(new MeshOctBox(vertices, new int[] { 0 }));
            meshoctboxes[0].FacetOverlap = 
                Enumerable.Range(0, facets.Count).ToList();

            // Subdivides based on overlapping facet count
            for(int idx = 0; idx < meshoctboxes.Count; idx++)
            {
                // If the number of facets for this octbox is above the max
                // and that the tree depth is below the maximum depth
                if(meshoctboxes[idx].FacetOverlap.Count > maxFacetCount &&
                    meshoctboxes[idx].Index.Length < maxTreeDepth)
                {
                    // Subdivides the current mesh octbox
                    meshoctboxes[idx].Subdivide();

                    // Processes each child box
                    foreach (MeshOctBox box in meshoctboxes[idx].ChildBoxes)
                    {
                        // Finds overlapping facets
                        box.FindFacetOverlap(vertices, facets, 
                            meshoctboxes[idx].FacetOverlap);

                        // Adds to mesh octree
                        meshoctboxes.Add(box);
                    }
                }
            }
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Returns a list of which mesh octboxes are currently on
        /// </summary>
        /// <returns> On status of each octbox </returns>
        public bool[] IsOn()
        {
            bool[] isOn = new bool[meshoctboxes.Count];
            for (int idx = 0; idx < meshoctboxes.Count; idx++)
                isOn[idx] = meshoctboxes[idx].On;
            return isOn;
        }

        /// <summary>
        /// Marks all children of the box at element "index"
        /// </summary>
        /// <param name="index"> Box's index </param>
        /// <returns> An array marking all child indices 
        /// (But not the box itself) </returns>
        public bool[] IsChild(int index)
        {
            // Finds last index rank and values
            var pBox = meshoctboxes[index];
            var pIdx = pBox.Index; // Parent index
            var pLen = pIdx.Length; // Parent Rank
            
            // Record
            var isChild = new bool[meshoctboxes.Count];

            // Marches through octree
            for (int idx = index + 1; idx < meshoctboxes.Count; ++idx)
            {
                var mbox = meshoctboxes[idx];

                // Extracts index
                var thisIdx = new int[pLen] ;
                Array.Copy(mbox.Index, 0, thisIdx, 0, pLen);

                // Determines if there is a match
                isChild[idx] = thisIdx.SequenceEqual(pIdx);
            }

            return isChild;
        }

        /// <summary>
        /// Returns a list of octbox intersection structures for matching
        /// the OctBox list of this MeshOctree
        /// </summary>
        /// <param name="ray"> Ray to intersect </param>
        /// <returns> List of octbox intersect structure corresponding to 
        /// the boxes in the mesh octbox list </returns>
        public List<OctBoxIntersect> Intersect(Ray ray)
        {
            List<OctBoxIntersect> octree = new List<OctBoxIntersect>();
            var skip = new bool[meshoctboxes.Count];


            // Steps through each box
            for (int idx = 0; idx < meshoctboxes.Count; idx++)
            {
                // Checks to see if this is a child of a missed box
                if (skip[idx]) continue;

                // Referenences box
                var mBox = meshoctboxes[idx];

                // Marks box in tree for skipping (Not necessary)
                skip[idx] = true;

                // Checks to see if this box is intesected
                var oData = mBox.Intersect(ray);
                var thisHit = oData.Hit;

                // If this box is not hit, then marks all children so
                // they are not traced
                if (!thisHit)
                {
                    var children = IsChild(idx);
                    skip = skip.Zip(children, (a, b) => a || b).ToArray();
                }

                // Only records if there are not no children
                if (thisHit && mBox.ChildBoxes == null)
                    octree.Add(oData);
            }

            return octree;
        }

        /// <summary>
        /// Deep-copy (Non-referenced) clone
        /// </summary>
        /// <returns> Cloned copy </returns>
        public MeshOctree Clone()
        {
            return CloneImp();
        }

        /// <summary>
        /// Deep-copy (Non-referenced) clone casted as an object class
        /// </summary>
        /// <returns> Object class clone </returns>
        object ICloneable.Clone()
        {
            return CloneImp();
        }

        /// <summary>
        /// Clone implementation. Uses MemberwiseClone to clone, and 
        /// inheriting classes will implement the cloning of
        /// specific data types 
        /// </summary>
        /// <returns> Clone copy </returns>
        protected virtual MeshOctree CloneImp()
        {
            // Shallow copy
            var newCopy = (MeshOctree)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);
            
            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected virtual void DeepCopyOverride(ref MeshOctree copy)
        {
            // protected internal List<MeshOctBox> mesh octboxes;
            if (meshoctboxes != null)
                copy.meshoctboxes = meshoctboxes.Select(c => c.Clone()).ToList();
        }

        #endregion Methods
    }
}
