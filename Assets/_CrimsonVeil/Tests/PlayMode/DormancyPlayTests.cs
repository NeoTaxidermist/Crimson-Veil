using System.Collections;
using System.Linq;
using CrimsonVeil.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// A chapter holds hundreds of creatures: one far out of the picture sleeps - nothing of it runs and it is out of
    /// the fight - and wakes before it comes into the picture. Its body is not re-skinned every frame to measure it.
    /// </summary>
    [Category("Combat")]
    public class DormancyPlayTests
    {
        GameObject cam, wraith;

        [SetUp]
        public void SetUp()
        {
            cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = 30f;
            cam.transform.position = new Vector3(0f, 2f, -18f);
            DormancyWatch.Enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (wraith != null) Object.Destroy(wraith);
            Object.Destroy(cam);
        }

        static IEnumerator Wait(float s)
        {
            float end = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator AWraithFarAway_Sleeps_OutOfTheFight_AndWakesBeforeItIsSeen()
        {
            wraith = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/LevelKit/Prefabs/Wraith.prefab"));
            wraith.transform.position = new Vector3(60f, 0f, 0f);
            var enemy = wraith.GetComponent<Enemy>();
            var dormancy = wraith.GetComponent<Dormancy>();
            Assert.IsNotNull(dormancy, "a creature has its dormancy without any setup");
            yield return Wait(DormancyWatch.Every * 2f + 0.1f);
            Assert.IsTrue(dormancy.Asleep, "60 m away, far out of the picture: asleep");
            Assert.IsFalse(enemy.enabled, "its mind does not run");
            Assert.IsFalse(wraith.GetComponent<EnemyAnimator>().enabled, "nor its animation");
            Assert.IsFalse(Hostiles.All.Contains(enemy), "and it is out of the fight");
            var smr = wraith.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsFalse(smr.updateWhenOffscreen, "its body is not re-skinned every frame to measure it");
            Assert.Greater(smr.localBounds.size.y, 2.5f, "its fixed bounds hold its poses");

            // it comes nearer: just beyond the picture's edge it is already awake
            var view = DormancyWatch.View(cam.GetComponent<Camera>()).Value;
            wraith.transform.position = new Vector3(view.xMax + DormancyWatch.WakeMargin - 1f, 0f, 0f);
            yield return Wait(DormancyWatch.Every * 2f + 0.1f);
            Assert.IsFalse(dormancy.Asleep, "near the edge: awake before it is seen");
            Assert.IsTrue(enemy.enabled && wraith.GetComponent<EnemyAnimator>().enabled, "everything runs again");
            Assert.IsTrue(Hostiles.All.Contains(enemy), "and it is in the fight again");
        }
    }
}
