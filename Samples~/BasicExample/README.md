# Basic Localization Example

这个示例展示最新的最小接入流程：`LocalizationSystem` 现在无参构建，并在首次使用时从 `Resources/Localization/LanguageConfig` 读取烘焙后的配置。

1. 将本 sample 从 Package Manager 导入项目。
2. 打开导入后的 `BasicExample` 文件夹，记下 `UI.csv` 所在目录。
3. 执行 `Tools/Localization/Open Language Config`，在 `Project Settings > Localization` 中将 `sourceFolderPath` 指向第 2 步的目录；将 `soFolderPath` 保持为 `Assets/Resources/Localization` 或项目约定目录。
4. 确认宿主项目已经初始化 Addressables，然后执行 `Tools/Localization/Convert All Source Files`。
5. 转换通过校验后，生成的 `UI.asset` 会自动加入 `Localization` Addressables 分组，无需手动添加。
6. 打开 `SampleScene` 并运行。场景已经包含 `BasicLocalizationExample` 和文本示例组件，不需要重新挂载。

`BasicLocalizationExample` 提供以下运行时入口：

```csharp
BasicLocalizationExample localization = BasicLocalizationExample.Instance;
localization.SetLanguage("en");
bool ready = localization.IsReady;
string languageCode = localization.GetLanguageCode();
string text = localization.GetLocalizedText("<UI|START_GAME>");
localization.AddListener(RefreshTexts);
```

`BasicLocalizationExample` 只是一个场景级门面，内部通过 `new LocalizationSystem()` 构建系统；业务代码也可以直接持有 `LocalizationSystem`。配置源保存在 `ProjectSettings/DotlineLocalizationSettings.asset`。打开 Project Settings、进入 Play Mode、执行转换或构建 Player 前都会确保运行时配置烘焙到 `Assets/Resources/Localization/LanguageConfig.asset`，对应运行时路径为 `Localization/LanguageConfig`。

示例模板：

```text
<UI|WELCOME(<UI|PLAYER_NAME>)>
```

预期英文输出：

```text
Welcome Captain
```
