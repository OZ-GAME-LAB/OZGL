using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using OzGameLab01.Combat;

namespace OzGameLab01.Tests.EditMode
{
    public class ProjectileAddressableTests
    {
        private const string ProjectileFolder = "Assets/_Project/02_Prefabs/VFX/Projectiles";

        [Test]
        public void ProjectilePrefabsOwnAddressableSpriteAndContainNoAdditionalVfx()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectileFolder });
            Assert.That(guids, Has.Length.EqualTo(20));

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Projectile projectile = prefab.GetComponent<Projectile>();

                Assert.That(projectile, Is.Not.Null, path);
                Assert.That(projectile.SpriteReference, Is.Not.Null, path);
                Assert.That(projectile.SpriteReference.RuntimeKeyIsValid(), Is.True, path);
                Assert.That(settings.FindAssetEntry(projectile.SpriteReference.AssetGUID), Is.Not.Null, path);
                Assert.That(prefab.GetComponentsInChildren<ParticleSystem>(true), Is.Empty, path);
                Assert.That(prefab.GetComponentsInChildren<Animator>(true), Is.Empty, path);
                Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(true).Single().sprite, Is.Null, path);
                Assert.That(settings.FindAssetEntry(guid), Is.Not.Null, path);
            }
        }

        [Test]
        public void ProductionUnitsReferenceAddressableProjectilePrefabs()
        {
            string[] roots =
            {
                "Assets/_Project/02_Prefabs/Resources/Characters/AllyPrefabs",
                "Assets/_Project/02_Prefabs/Resources/Characters/EnemyPrefabs"
            };
            string[] guids = AssetDatabase.FindAssets("t:Prefab", roots);
            Assert.That(guids, Has.Length.EqualTo(19));

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Unit unit = prefab.GetComponentInChildren<Unit>(true);
                var serialized = new SerializedObject(unit);
                string projectileGuid = serialized.FindProperty("projectilePrefabReference")
                    .FindPropertyRelative("m_AssetGUID").stringValue;

                Assert.That(projectileGuid, Is.Not.Empty, path);
                Assert.That(settings.FindAssetEntry(projectileGuid), Is.Not.Null, path);
                GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(projectileGuid));
                Assert.That(projectilePrefab.GetComponent<Projectile>(), Is.Not.Null, path);
            }
        }

        [Test]
        public void BasicAttackPresentationDoesNotInstantiateExtraEffectPrefabs()
        {
            var effect = new GameObject("BasicExtraEffect");
            try
            {
                var presenter = new UnitPresenter(null, null, null, null,
                    effect, effect, null, null, 0.35f, 0.5f, 0.75f);
                presenter.PlayAttackEffect(Vector3.zero, isBasicAttack: true);
                presenter.PlayHitEffect(Vector3.zero, isBasicAttack: true);
                Assert.That(GameObject.Find("BasicExtraEffect(Clone)"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(effect);
            }
        }
    }
}
