using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using Localization.Editor;

namespace Localization.Tests
{
    /// <summary>
    /// 验证 LanguageDataSO 自动注册 Addressables 时依赖 ProjectSettings 中配置的分组名。
    /// </summary>
    public class LanguageDataSOAddressableRegistrarTests
    {
        private const string TempFolder = "Assets/TempLanguageRegistrarTests";
        private const string TestAssetName = "RegistrarTestNs";
        private const string TestGroup = "LocalizationRegistrarTestGroup";
        private const string TestGroupAlt = "LocalizationRegistrarTestGroupAlt";

        private string _originalGroupName;
        private AddressableAssetSettings _settings;
        private LanguageDataSO _data;
        private string _assetGuid;

        [SetUp]
        public void SetUp()
        {
            var projectSettings = LanguageProjectSettings.GetOrCreate();
            _originalGroupName = projectSettings.languageDataAddressablesGroupName;

            _settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            Assume.That(_settings, Is.Not.Null, "Addressables settings should exist in this project.");

            if (!AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.CreateFolder("Assets", "TempLanguageRegistrarTests");

            _data = ScriptableObject.CreateInstance<LanguageDataSO>();
            AssetDatabase.CreateAsset(_data, $"{TempFolder}/{TestAssetName}.asset");
            _data.entries = new List<LocalizationData>
            {
                new()
                {
                    key = "TEST_KEY_1",
                    texts = new[] { new LocalizationText { languageCode = "zh-Hans", text = "测试" } }
                }
            };
            EditorUtility.SetDirty(_data);
            AssetDatabase.SaveAssets();

            _assetGuid = AssetDatabase.AssetPathToGUID($"{TempFolder}/{TestAssetName}.asset");
            Assume.That(_assetGuid, Is.Not.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null)
            {
                var entry = _settings.FindAssetEntry(_assetGuid);
                if (entry != null)
                    entry.parentGroup?.RemoveAssetEntry(entry);

                var testGroup = _settings.FindGroup(TestGroup);
                if (testGroup != null)
                    _settings.RemoveGroup(testGroup);

                var testGroupAlt = _settings.FindGroup(TestGroupAlt);
                if (testGroupAlt != null)
                    _settings.RemoveGroup(testGroupAlt);
            }

            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            var projectSettings = LanguageProjectSettings.GetOrCreate();
            projectSettings.languageDataAddressablesGroupName = _originalGroupName;
            projectSettings.SaveProjectSettings();
            AssetDatabase.SaveAssets();
        }

        [Test]
        public void Register_UsesConfiguredGroupName()
        {
            var projectSettings = LanguageProjectSettings.GetOrCreate();
            projectSettings.languageDataAddressablesGroupName = TestGroup;
            projectSettings.SaveProjectSettings();

            Assert.IsTrue(
                LanguageDataSOAddressableRegistrar.TryRegisterIfValid(_data),
                "a valid LanguageDataSO should register successfully");

            var group = _settings.FindGroup(TestGroup);
            Assert.IsNotNull(group, $"group '{TestGroup}' should be created from ProjectSettings");
            Assert.IsNotNull(group.GetSchema<BundledAssetGroupSchema>(), "group should carry BundledAssetGroupSchema");
            Assert.IsNotNull(group.GetSchema<ContentUpdateGroupSchema>(),
                "group should carry ContentUpdateGroupSchema");

            var entry = _settings.FindAssetEntry(_assetGuid);
            Assert.IsNotNull(entry, "asset should have an Addressable entry");
            Assert.AreEqual(TestGroup, entry.parentGroup.name, "entry should live in the configured group");
            Assert.AreEqual(TestAssetName, entry.address, "address should be the namespace id");
        }

        [Test]
        public void Register_MovesEntryWhenGroupNameChanges()
        {
            var projectSettings = LanguageProjectSettings.GetOrCreate();
            projectSettings.languageDataAddressablesGroupName = TestGroup;
            projectSettings.SaveProjectSettings();

            Assert.IsTrue(LanguageDataSOAddressableRegistrar.TryRegisterIfValid(_data));
            Assert.AreEqual(
                TestGroup,
                _settings.FindAssetEntry(_assetGuid)?.parentGroup.name);

            projectSettings.languageDataAddressablesGroupName = TestGroupAlt;
            projectSettings.SaveProjectSettings();

            Assert.IsTrue(LanguageDataSOAddressableRegistrar.TryRegisterIfValid(_data));
            Assert.AreEqual(
                TestGroupAlt,
                _settings.FindAssetEntry(_assetGuid)?.parentGroup.name,
                "entry should move to the newly configured group");
        }

        [Test]
        public void UnsetGroupName_SkipsRegistration()
        {
            var projectSettings = LanguageProjectSettings.GetOrCreate();
            projectSettings.languageDataAddressablesGroupName = "   ";
            projectSettings.SaveProjectSettings();
            Assert.AreEqual(string.Empty, projectSettings.GetLanguageDataAddressablesGroupName());

            Assert.IsFalse(
                LanguageDataSOAddressableRegistrar.TryRegisterIfValid(_data),
                "registration must be skipped while no group is configured");
            Assert.IsNull(_settings.FindAssetEntry(_assetGuid), "unset group must not create any entry");
        }

        [Test]
        public void InvalidKey_IsNotRegistered()
        {
            _data.entries = new List<LocalizationData>
            {
                new()
                {
                    key = "invalid_key",
                    texts = new[] { new LocalizationText { languageCode = "zh-Hans", text = "测试" } }
                }
            };

            Assert.IsFalse(
                LanguageDataSOAddressableRegistrar.TryRegisterIfValid(_data),
                "keys must be UPPER_SNAKE_CASE to register");
            Assert.IsNull(_settings.FindAssetEntry(_assetGuid), "invalid data must not become addressable");
        }
    }
}