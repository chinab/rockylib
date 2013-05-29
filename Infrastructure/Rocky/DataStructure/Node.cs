using System;

namespace Rocky.DataStructure
{
    /// <summary>
    /// The Node&lt;T&gt; class represents the base concept of a Node for a tree or graph. 
    /// It contains a data item of type T, and a list of neighbors.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the Node.</typeparam>
    public class Node<T>
    {
        #region Fields
        private T data;
        private NodeList<T> neighbors;
        #endregion

        #region Properties
        public T Value
        {
            get { return data; }
            protected internal set { data = value; }
        }
        protected NodeList<T> Neighbors
        {
            get { return neighbors; }
            set { neighbors = value; }
        }
        #endregion

        #region Constructors
        protected internal Node()
        {

        }
        public Node(T data)
            : this(data, null)
        {

        }
        public Node(T data, NodeList<T> neighbors)
        {
            this.data = data;
            this.neighbors = neighbors;
        }
        #endregion
    }
}