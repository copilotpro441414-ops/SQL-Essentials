using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace SqlEssentials.Core.Tests.Shared
{
    /// <summary>
    /// Shared test assertions and helper utilities for deterministic testing.
    /// </summary>
    public static class TestAssertions
    {
        /// <summary>
        /// Asserts that two collections contain the same elements, ignoring order.
        /// </summary>
        public static void AssertEquivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null)
        {
            var expectedList = expected?.ToList() ?? new List<T>();
            var actualList = actual?.ToList() ?? new List<T>();

            Assert.Equal(expectedList.Count, actualList.Count);
            
            foreach (var item in expectedList)
            {
                Assert.Contains(item, actualList);
            }
        }

        /// <summary>
        /// Asserts that a collection is ordered according to the specified comparer.
        /// </summary>
        public static void AssertOrdered<T>(IEnumerable<T> collection, IComparer<T> comparer = null, string message = null)
        {
            var list = collection?.ToList() ?? new List<T>();
            if (list.Count <= 1) return;

            var actualComparer = comparer ?? Comparer<T>.Default;

            for (int i = 0; i < list.Count - 1; i++)
            {
                var compareResult = actualComparer.Compare(list[i], list[i + 1]);
                Assert.True(compareResult <= 0, 
                    message ?? $"Collection not ordered: element at index {i} ({list[i]}) is greater than element at index {i + 1} ({list[i + 1]})");
            }
        }

        /// <summary>
        /// Asserts that a value falls within an expected range.
        /// </summary>
        public static void AssertInRange<T>(T value, T min, T max, string message = null) where T : IComparable<T>
        {
            Assert.True(value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0, 
                message ?? $"Value {value} is not in range [{min}, {max}]");
        }

        /// <summary>
        /// Asserts that an action completes within a specified time.
        /// </summary>
        public static void AssertCompletesWithin(Action action, TimeSpan maxDuration, string message = null)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            
            Assert.True(stopwatch.Elapsed <= maxDuration, 
                message ?? $"Action took {stopwatch.Elapsed.TotalMilliseconds}ms, expected <= {maxDuration.TotalMilliseconds}ms");
        }
    }
}
