using System;
using System.Collections.Generic;

namespace KombiRush.Tests
{
    /// <summary>Tiny assert harness so the sim can be tested without Unity or NUnit.</summary>
    public static class Harness
    {
        private static readonly List<string> Failures = new List<string>();
        private static int _passed;
        private static string _current = "";

        public static void Test(string name, Action body)
        {
            _current = name;
            try
            {
                body();
                _passed++;
                Console.WriteLine("  PASS  " + name);
            }
            catch (AssertException ex)
            {
                Failures.Add(name + ": " + ex.Message);
                Console.WriteLine("  FAIL  " + name + "\n          " + ex.Message);
            }
            catch (Exception ex)
            {
                Failures.Add(name + ": threw " + ex.GetType().Name + " " + ex.Message);
                Console.WriteLine("  ERROR " + name + "\n          " + ex);
            }
        }

        public static int Summary()
        {
            Console.WriteLine();
            Console.WriteLine("checks passed: " + _passed + ", failed: " + Failures.Count);
            foreach (string f in Failures) Console.WriteLine("  - " + f);
            return Failures.Count == 0 ? 0 : 1;
        }

        public static void True(bool condition, string message)
        {
            if (!condition) throw new AssertException(message);
        }

        public static void Equal(int expected, int actual, string message)
        {
            if (expected != actual) throw new AssertException(message + " (expected " + expected + ", got " + actual + ")");
        }

        public static void Equal(string expected, string actual, string message)
        {
            if (expected != actual) throw new AssertException(message + " (expected '" + expected + "', got '" + actual + "')");
        }

        public static void Near(float expected, float actual, float tolerance, string message)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new AssertException(message + " (expected " + expected + " +/- " + tolerance + ", got " + actual + ")");
        }

        public static void Greater(float actual, float threshold, string message)
        {
            if (!(actual > threshold)) throw new AssertException(message + " (expected > " + threshold + ", got " + actual + ")");
        }

        public static void AtMost(float actual, float threshold, string message)
        {
            if (!(actual <= threshold)) throw new AssertException(message + " (expected <= " + threshold + ", got " + actual + ")");
        }

        private sealed class AssertException : Exception
        {
            public AssertException(string message) : base(message) { }
        }
    }
}
