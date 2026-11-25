#nullable enable
using System.Collections;
using EmpathyLab.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmpathyLab.Tests.PlayMode
{
    /// <summary>Scene-level checks. They drive the presenter API, so CI does not need a headset.</summary>
    public sealed class EmpathyLabPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("EmpathyLab", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneLoads()
        {
            Assert.AreEqual("EmpathyLab", SceneManager.GetActiveScene().name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator XrRigExistsAndHasACamera()
        {
            var origin = GameObject.Find(XrRigBootstrap.OriginObjectName);
            Assert.IsNotNull(origin, "XR Origin is missing from the scene.");
            Assert.IsNotNull(origin.GetComponentInChildren<Camera>(), "XR Origin has no camera.");
            Assert.IsNotNull(Object.FindAnyObjectByType<XrRigBootstrap>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullScenarioRunShowsTheDebrief()
        {
            var presenter = Object.FindAnyObjectByType<ScenarioPresenter>();
            Assert.IsNotNull(presenter);

            var guard = 0;
            while (!presenter.IsReady && guard < 30)
            {
                guard++;
                yield return null;
            }

            Assert.IsTrue(presenter.IsReady, presenter.Error);
            Assert.IsNull(presenter.Error, presenter.Error);

            guard = 0;
            while (!presenter.IsComplete && guard < 12)
            {
                presenter.ChooseFirst();
                guard++;
                yield return null;
            }

            Assert.IsTrue(presenter.IsComplete, "The scenario did not complete.");
            Assert.IsTrue(presenter.IsDebriefVisible, "The debrief panel is hidden.");
            Assert.IsNotNull(presenter.Debrief);
            Assert.Greater(presenter.Debrief!.Choices.Count, 0);
        }
    }
}
