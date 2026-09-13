# Basic Localization Example

这个示例展示最小接入流程：

1. 将本 sample 从 Package Manager 导入项目。
2. 打开导入后的 `BasicExample` 文件夹，记下 `UI.csv` 所在目录。
3. 选中导入后的 `LanguageConfig.asset`，将 `sourceFolderPath` 指向第 2 步的目录；将 `soFolderPath` 保持为 `Assets/Resources/Localization` 或项目约定目录。
4. 确认宿主项目已经初始化 Addressables，然后执行 `Tools/Localization/Convert All Source Files`。
5. 转换通过校验后，生成的 `UI.asset` 会自动加入 `Localization` Addressables 分组，无需手动添加。
6. 打开 `SampleScene` 并运行。场景已经包含 `BasicLocalizationExample` 和文本示例组件，不需要重新挂载。

示例模板：

```text
<UI|WELCOME(<UI|PLAYER_NAME>)>
```

预期英文输出：

```text
Welcome Captain
```
