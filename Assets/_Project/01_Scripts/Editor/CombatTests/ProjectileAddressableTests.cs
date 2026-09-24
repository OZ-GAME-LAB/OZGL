using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using OzGameLab01.Combat;

namespace OzGameLab01.Tests.EditMode
{
    public class ProjectileAddressableTests
    {
        private const string ProjectileFolder = "Assets/_Project/02_Prefabs/VFX/Projectiles";

        [Test]
        public void ProjectilePrefabsOwnAddressableSpriteAndContainNoEmbeddedVfx()
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
                AssertAddressableReferenceRegistered(projectile.TravelEffectReference, settings, path);
                AssertAddressableReferenceRegistered(projectile.ImpactEffectReference, settings, path);
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
                Projectile projectile = projectilePrefab.GetComponent<Projectile>();
                Assert.That(projectile, Is.Not.Null, path);

                string expectedTravelGuid = AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(serialized.FindProperty("attackEffectPrefab").objectReferenceValue));
                string expectedImpactGuid = AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(serialized.FindProperty("hitEffectPrefab").objectReferenceValue));
                Assert.That(projectile.TravelEffectReference?.AssetGUID ?? string.Empty,
                    Is.EqualTo(expectedTravelGuid), path + " travel VFX");
                Assert.That(projectile.ImpactEffectReference?.AssetGUID ?? string.Empty,
                    Is.EqualTo(expectedImpactGuid), path + " impact VFX");
            }
        }

        [Test]
        public void DocumentationBackedProjectileSpriteMappingsAreWired()
        {
            var expected = new (string Projectile, string Sprite)[]
            {
                ("Projectile_Ally_CheshireCat", "Effect_Battle_Magicmissile"),
                ("Projectile_Ally_Dorothy", "Effect_Battle_Halfmoon"),
                ("Projectile_Ally_Mermaid", "Effect_Battle_Soundnote"),
                ("Projectile_Ally_PeterPan", "Effect_Battle_Arrow"),
                ("Projectile_Ally_Rabbit", "Effect_Battle_Cloack"),
                ("Projectile_Ally_Snowwhite", "Effect_Battle_Badapple"),
                ("Projectile_Ally_Tinkerbell", "Effect_Battle_Heart"),
                ("Projectile_Enemy_Book", "Effect_Battle_Talisman"),
                ("Projectile_Enemy_CandyMonster", "Effect_Battle_Cookie"),
                ("Projectile_Enemy_Crow", "Effect_Battle_Feather"),
                ("Projectile_Enemy_Hunter", "Effect_Battle_Arrow"),
                ("Projectile_Enemy_ToySoldier", "Effect_Battle_Bullet"),
                ("Projectile_Enemy_Wolf", "Effect_Battle_Claw"),
            };

            foreach (var mapping in expected)
            {
                string path = $"{ProjectileFolder}/{mapping.Projectile}.prefab";
                Projectile projectile = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Projectile>();
                string spritePath = AssetDatabase.GUIDToAssetPath(projectile.SpriteReference.AssetGUID);
                Assert.That(System.IO.Path.GetFileNameWithoutExtension(spritePath),
                    Is.EqualTo(mapping.Sprite), path);
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

        private static void AssertAddressableReferenceRegistered(
            AssetReferenceGameObject reference,
            AddressableAssetSettings settings,
            string context)
        {
            if (reference == null || !reference.RuntimeKeyIsValid()) return;
            Assert.That(settings.FindAssetEntry(reference.AssetGUID), Is.Not.Null, context);
        }
    }
}
