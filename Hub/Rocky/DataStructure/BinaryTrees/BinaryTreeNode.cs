using System;
using System.Collections;
using System.Collections.Generic;

namespace System.DataStructure
{
    /// <summary>
    /// The BinaryTreeNode class represents a node in a binary tree, or a binary search tree.
    /// It has precisely two neighbors, which can be accessed via the Left and Right properties.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the binary tree node.</typeparam>
    public class BinaryTreeNode<T> : Node<T>
    {
        #region Properties
        public BinaryTreeNode<T> Left
        {
            get
            {
                if (base.Neighbors == null)
                {
                    return null;
                }
                return (BinaryTreeNode<T>)base.Neighbors[0];
            }
            set
            {
                if (base.Neighbors == null)
                {
                    base.Neighbors = new NodeList<T>(2);
                }
                base.Neighbors[0] = value;
            }
        }

        public BinaryTreeNode<T> Right
        {
            get
            {
                if (base.Neighbors == null)
                {
                    return null;
                }
                return (BinaryTreeNode<T>)base.Neighbors[1];
            }
            set
            {
                if (base.Neighbors == null)
                {
                    base.Neighbors = new NodeList<T>(2);
                }
                base.Neighbors[1] = value;
            }
        }
        #endregion

        #region Constructors
        public BinaryTreeNode(T data)
            : base(data, null)
        {

        }
        public BinaryTreeNode(T data, BinaryTreeNode<T> left, BinaryTreeNode<T> right)
        {
            base.Value = data;
            NodeList<T> children = new NodeList<T>(2);
            children[0] = left;
            children[1] = right;
            base.Neighbors = children;
        }
        #endregion
    }
}