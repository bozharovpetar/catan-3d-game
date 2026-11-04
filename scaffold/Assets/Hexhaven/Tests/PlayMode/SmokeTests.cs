// SCAFFOLD — PlayMode smoke test skeleton. Implement P01–P08 from docs/09-TESTING.md §4.
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hexhaven.Tests
{
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator P01_MainSceneBootsWithoutErrors()
        {
            SceneManager.LoadScene("Main");
            float t = 0f;
            while (t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(Object.FindAnyObjectByType<HexhavenApp>());
            LogAssert.NoUnexpectedReceived();
        }
    }
}
