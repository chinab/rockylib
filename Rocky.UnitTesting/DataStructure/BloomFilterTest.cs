using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.DataStructure;

namespace Rocky.UnitTesting
{
    [TestClass]
    public class BloomFilterTest
    {
        /// <summary>
        /// There should be no false negatives.
        /// </summary>
        [TestMethod()]
        public void NoFalseNegativesTest()
        {
            // set filter properties
            int capacity = 10000;
            float errorRate = 0.001F; // 0.1%

            // create input collection
            var inputs = generateRandomDataList(capacity);

            // instantiate filter and populate it with the inputs
            var target = new BloomFilter<string>(capacity, errorRate);
            foreach (string input in inputs)
            {
                target.Add(input);
            }

            // check for each input. if any are missing, the test failed
            foreach (string input in inputs)
            {
                if (!target.Contains(input))
                {
                    Assert.Fail("False negative: {0}", input);
                }
            }
        }
        private static List<String> generateRandomDataList(int capacity)
        {
            var inputs = new List<string>(capacity);
            for (uint i = 0; i < capacity; i++)
            {
                inputs.Add(Guid.NewGuid().ToString());
            }
            return inputs;
        }

        /// <summary>
        /// Only in extreme cases should there be a false positive with this test.
        /// </summary>
        [TestMethod()]
        public void LowProbabilityFalseTest()
        {
            int capacity = 10000; // we'll actually add only one item
            float errorRate = 0.0001F; // 0.01%

            // instantiate filter and populate it with a single random value
            var target = new BloomFilter<string>(capacity, errorRate);
            target.Add(Guid.NewGuid().ToString());

            // generate a new random value and check for it
            if (target.Contains(Guid.NewGuid().ToString()))
            {
                Assert.Fail("Check for missing item returned true.");
            }
        }

        [TestMethod()]
        public void FalsePositivesInRangeTest()
        {
            // set filter properties
            int capacity = 1000000;
            float errorRate = 0.001F; // 0.1%

            // instantiate filter and populate it with random strings
            var target = new BloomFilter<string>(capacity, errorRate);
            for (int i = 0; i < capacity; i++)
            {
                target.Add(Guid.NewGuid().ToString());
            }

            // generate new random strings and check for them
            // about errorRate of them should return positive
            int falsePositives = 0;
            int testIterations = capacity;
            int expectedFalsePositives = (int)(testIterations * errorRate) * 2;
            for (int i = 0; i < testIterations; i++)
            {
                string test = Guid.NewGuid().ToString();
                if (target.Contains(test))
                {
                    falsePositives++;
                }
            }

            if (falsePositives > expectedFalsePositives)
            {
                Assert.Fail("Number of false positives ({0}) greater than expected ({1}).", falsePositives, expectedFalsePositives);
            }
        }

        [TestMethod()]
        public void LargeInputTest()
        {
            // set filter properties
            int capacity = 2000000;
            float errorRate = 0.01F; // 1%

            // instantiate filter and populate it with random strings
            var target = new BloomFilter<string>(capacity, errorRate);
            for (int i = 0; i < capacity; i++)
                target.Add(Guid.NewGuid().ToString());

            // if it didn't error out on that much input, this test succeeded
        }

        [TestMethod()]
        public void LargeInputTestAutoError()
        {
            // set filter properties
            int capacity = 2000000;

            // instantiate filter and populate it with random strings
            var target = new BloomFilter<string>(capacity);
            for (int i = 0; i < capacity; i++)
                target.Add(Guid.NewGuid().ToString());

            // if it didn't error out on that much input, this test succeeded
        }

        /// <summary>
        /// If k and m are properly choses for n and the error rate, the filter should be about half full.
        /// </summary>
        [TestMethod()]
        public void TruthinessTest()
        {
            int capacity = 10000;
            float errorRate = 0.001F; // 0.1%
            var target = new BloomFilter<string>(capacity, errorRate);
            for (int i = 0; i < capacity; i++)
                target.Add(Guid.NewGuid().ToString());

            double actual = target.Truthiness;
            double expected = 0.5;
            double threshold = 0.01; // filter shouldn't be < 49% or > 51% "true"
            Assert.IsTrue(Math.Abs(actual - expected) < threshold, "Information density too high or low. Actual={0}, Expected={1}", actual, expected);
        }

        //[TestMethod()]
        //[ExpectedException(typeof(OverflowException))]
        //public void OverLargeInputTest()
        //{
        //    // set filter properties
        //    int capacity = int.MaxValue - 1;
        //    float errorRate = 0.01F; // 1%

        //    // instantiate filter
        //    var target = new BloomFilter<string>(capacity, errorRate);
        //}
    }
}