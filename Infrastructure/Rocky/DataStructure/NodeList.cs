using System;
using System.Collections.ObjectModel;

namespace Rocky.DataStructure
{
    /// <summary>
    /// Represents a collection of Node&lt;T&gt; instances.
    /// </summary>
    /// <typeparam name="T">The type of data held in the Node instances referenced by this class.</typeparam>
    public class NodeList<T> : Collection<Node<T>>
    {
        #region Constructors
        public NodeList()
            : base()
        {

        }
        public NodeList(int initialSize)
        {
            for (int i = 0; i < initialSize; i++)
            {
                base.Items.Add(default(Node<T>));
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Searches the NodeList for a Node containing a particular value.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The Node in the NodeList, if it exists; null otherwise.</returns>
        public Node<T> Find(T value)
        {
            foreach (Node<T> node in base.Items)
            {
                if (node.Value.Equals(value))
                {
                    return node;
                }
            }
            return null;
        }
        #endregion
    }
}