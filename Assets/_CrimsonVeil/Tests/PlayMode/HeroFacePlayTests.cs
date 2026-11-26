using System.Collections;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrimsonVeil.Tests
{
    /// <summary>The face on the real model: the jaw drops, the lids shut, the brows rise, the eyes follow a point.</summary>
    [Category("Hero")]
    public class HeroFacePlayTests
    {
        GameObject hero;
        HeroFace face;

        [SetUp]
        public void SetUp()
        {
            hero = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Hero/Hero.fbx"));
            hero.transform.position = new Vector3(1000f, 200f, 0f);
            face = hero.AddComponent<HeroFace>();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(hero);

        static Transform Bone(GameObject go, string n) => CrimsonVeil.Core.RigJoint.FindDeep(go.transform, n);

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator Surprised_TheJawDrops_TheBrowsRise()
        {
            Assert.IsTrue(face.IsReady, "the model has its face rig");
            var chin = Bone(hero, "Jaw");
            var brow = Bone(hero, "Brow.L");
            yield return Frames(2);
            Vector3 chinTip0 = chin.position + chin.up * 0.07f;
            float brow0 = brow.position.y;
            face.SetExpression(FaceExpression.Surprised, 0.05f);
            yield return Frames(20);
            Vector3 chinTip1 = chin.position + chin.up * 0.07f;
            Assert.Less(chinTip1.y, chinTip0.y - 0.003f, "the chin drops");
            Assert.Greater(brow.position.y, brow0 + 0.003f, "the brow rises");
        }

        [UnityTest]
        public IEnumerator ABlink_ShutsTheLids_AndOpensThemAgain()
        {
            var lid = Bone(hero, "Lid.U.L");
            yield return Frames(2);
            Vector3 edge0 = lid.position + lid.up * 0.02f;      // the lid's front
            face.Blink();
            float lowest = edge0.y;
            for (int i = 0; i < 20; i++)
            {
                yield return null;
                lowest = Mathf.Min(lowest, (lid.position + lid.up * 0.02f).y);
            }
            Assert.Less(lowest, edge0.y - 0.008f, "the upper lid came down over the eye");
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(edge0.y, (lid.position + lid.up * 0.02f).y, 0.003f, "and is open again");
        }

        [UnityTest]
        public IEnumerator TheEyes_FollowAPoint()
        {
            var eye = Bone(hero, "Eye.L");
            yield return Frames(2);
            Vector3 ahead = eye.up;   // the eye looks along its bone
            // a point well to one side of where he faces
            Vector3 side = Vector3.Cross(Vector3.up, ahead).normalized;
            face.LookAt(eye.position + ahead * 1f + side * 0.5f);
            yield return Frames(30);
            Assert.Greater(Vector3.Dot(eye.up, side), 0.25f, "the eye has turned towards it");
        }
    }
}
