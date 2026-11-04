// Minimal Unity Test Framework surface for tools/compile-check.sh. NOT shipped in the game.
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UnityTestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class UnitySetUpAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class UnityTearDownAttribute : Attribute { }

    public static class LogAssert
    {
        public static bool ignoreFailingMessages;
        public static void Expect(LogType type, string message) { }
        public static void Expect(LogType type, Regex message) { }
        public static void NoUnexpectedReceived() { }
    }
}
