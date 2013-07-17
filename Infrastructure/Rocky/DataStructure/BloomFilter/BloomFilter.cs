using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace System.DataStructure
{
    public sealed class BloomFilter<T>
    {
        #region NestedTypes
        /// <summary>
        /// A function that can be used to hash input.
        /// </summary>
        /// <param name="input">The values to be hashed.</param>
        /// <returns>The resulting hash code.</returns>
        public delegate int HashFunction(T input);
        #endregion

        #region Static
        /// <summary>
        /// Hashes a string using Bob Jenkin's "One At A Time" method from Dr. Dobbs (http://burtleburtle.net/bob/hash/doobs.html).
        /// Runtime is suggested to be 9x+9, where x = input.Length. 
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int hashString(T input)
        {
            string s = input as string;
            int hash = 0;
            for (int i = 0; i < s.Length; i++)
            {
                hash += s[i];
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            return hash;
        }

        /// <summary>
        /// Hashes a 32-bit signed int using Thomas Wang's method v3.1 (http://www.concentric.net/~Ttwang/tech/inthash.htm).
        /// Runtime is suggested to be 11 cycles. 
        /// </summary>
        /// <param name="input">The integer to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int hashInt32(T input)
        {
            uint? x = input as uint?;
            unchecked
            {
                x = ~x + (x << 15); // x = (x << 15) - x- 1, as (~x) + y is equivalent to y - x - 1 in two's complement representation
                x = x ^ (x >> 12);
                x = x + (x << 2);
                x = x ^ (x >> 4);
                x = x * 2057; // x = (x + (x << 3)) + (x<< 11);
                x = x ^ (x >> 16);
                return (int)x;
            }
        }
        #endregion

        #region Fields
        private IBitVector _bitVector;
        private int _hashFunctionCount;
        private HashFunction _getHashSecondary;
        #endregion

        #region Properties
        public IBitVector BitVector
        {
            get { return _bitVector; }
        }
        /// <summary>
        /// The ratio of false to true bits in the filter. E.g., 1 true bit in a 10 bit filter means a truthiness of 0.1.
        /// </summary>
        public double Truthiness
        {
            get { return (double)_bitVector.Count / _bitVector.Capacity; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        public BloomFilter(int capacity) : this(capacity, null) { }
        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        public BloomFilter(int capacity, float errorRate) : this(capacity, errorRate, null, null) { }

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public BloomFilter(int capacity, HashFunction hashFunction) : this(capacity, BloomFilter.BestErrorRate(capacity), null, hashFunction) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="vectorFactory">vectorFactory</param>
        public BloomFilter(int capacity, float errorRate, Func<uint, IBitVector> vectorFactory, HashFunction hashFunction)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must be > 0");
            }
            if (errorRate >= 1 || errorRate <= 0)
            {
                throw new ArgumentOutOfRangeException("errorRate", errorRate, string.Format("errorRate must be between 0 and 1, exclusive. Was {0}", errorRate));
            }

            uint m = BloomFilter.ComputeM(capacity, errorRate);
            if (m < 1)
            {
                throw new OverflowException(string.Format("The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {0}, Error rate: {1}", capacity, errorRate));
            }
            if (vectorFactory == null)
            {
                _bitVector = new BitVector(m);
            }
            else
            {
                _bitVector = vectorFactory(m);
            }

            if (hashFunction == null)
            {
                if (typeof(T) == typeof(String))
                {
                    _getHashSecondary = hashString;
                }
                else if (typeof(T) == typeof(int))
                {
                    _getHashSecondary = hashInt32;
                }
                else
                {
                    throw new ArgumentNullException("hashFunction", "Please provide a hash function for your type T, when T is not a string or int.");
                }
            }
            else
            {
                _getHashSecondary = hashFunction;
            }
            int k = BloomFilter.ComputeK(capacity, errorRate);
            if (k < 1)
            {
                throw new OverflowException(string.Format("The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {0}, Error rate: {1}", capacity, errorRate));
            }
            _hashFunctionCount = k;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new item to the filter. It cannot be removed.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = _getHashSecondary(item);
            for (int i = 0; i < _hashFunctionCount; i++)
            {
                uint hash = computeHash(primaryHash, secondaryHash, i);
                _bitVector[hash] = true;
            }
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = _getHashSecondary(item);
            for (int i = 0; i < _hashFunctionCount; i++)
            {
                uint hash = computeHash(primaryHash, secondaryHash, i);
                if (!_bitVector[hash])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Performs Dillinger and Manolios double hashing. 
        /// </summary>
        private uint computeHash(int primaryHash, int secondaryHash, int i)
        {
            long resultingHash = (primaryHash + (i * secondaryHash)) % _bitVector.Capacity;
            return (uint)Math.Abs(resultingHash);
        }
        #endregion
    }

    #region BloomFilterAlgorithm
    /// <summary>
    /// 公式详见 http://en.wikipedia.org/wiki/Bloom_filter
    /// </summary>
    internal static class BloomFilter
    {
        /// <summary>
        /// 根据要容纳的元素数n和误判率p计算HashFunction的个数k
        /// k = m / n * ln2
        /// </summary>
        /// <param name="capacity">元素数n</param>
        /// <param name="errorRate">误判率p</param>
        /// <returns>HashFunction的个数k</returns>
        public static int ComputeK(int capacity, float errorRate)
        {
            return (int)Math.Round(ComputeM(capacity, errorRate) / capacity * Math.Log(2.0));
        }

        /// <summary>
        /// 根据要容纳的元素数n和误判率p计算BitVector的容量m
        /// m = -((n * lnp) / Pow(ln2, 2))
        /// </summary>
        /// <param name="capacity">元素数n</param>
        /// <param name="errorRate">误判率p</param>
        /// <returns>BitVector的容量m</returns>
        public static uint ComputeM(int capacity, float errorRate)
        {
            return (uint)Math.Ceiling(capacity * Math.Log(errorRate, (1.0 / Math.Pow(2, Math.Log(2.0)))));
        }

        /// <summary>
        /// 根据要容纳的元素数n计算误判率p
        /// </summary>
        /// <param name="capacity">元素数n</param>
        /// <returns>误判率p</returns>
        public static float BestErrorRate(int capacity)
        {
            float p = (float)(1.0 / capacity);
            if (p == 0)
            {
                p = (float)Math.Pow(0.6185, int.MaxValue / capacity); //http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
            }
            return p;
        }
    }
    #endregion
}