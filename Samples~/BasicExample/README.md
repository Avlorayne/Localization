# Basic Localization Example

这个示例展示最小接入流程：

1. 将本 sample 从 Package Manager 导入项目。
2. 打开 `Tools/Localization/Open Language Config`。
3. 将 `sourceFolderPath` 指向导入后的 `LocalizationSource` 目录。
4. 将 `soFolderPath` 保持在 `Assets/Resources/Localization` 或你的项目约定目录。
5. 执行 `Tools/Localization/Convert Changed Source Files`。
6. 将生成的 `UI.asset` 配置为 Addressable，地址建议使用 `UI`。
7. 把 `BasicLocalizationExample` 挂到场景对象上，绑定 `LanguageConfigSO`，通过 Context Menu 打印不同语言结果。

示例模板：

```text
<UI|WELCOME(<UI|PLAYER_NAME>)>
```

预期英文输出：

```text
Welcome Captain
```

