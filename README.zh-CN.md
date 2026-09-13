# Unity 本地化包(Localization)

[![Version](https://img.shields.io/badge/version-1.0.0-blue)](CHANGELOG.md)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity&logoColor=white)](https://unity.com/releases/editor/archive)
[![Addressables](https://img.shields.io/badge/Addressables-1.22.3-orange)](https://docs.unity3d.com/Packages/com.unity.addressables@1.22/manual/index.html)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE.md)

[English](README.md) | **简体中文**

`com.dotline.localization` 是一个面向 Unity 项目的轻量本地化数据包。它将 `.csv` / `.xlsx` 源表转换为 `LanguageDataSO` 资产,校验 Key 的合法性,在运行时解析嵌套文本模板,并通过 Addressables 加载语言数据。

## 功能概览

| 领域 | 说明 |
| --- | --- |
| 配置 | 一个 `LanguageConfigSO` 统一管理源文件目录、生成的 `LanguageDataSO` 目录、默认语言与 fallback 语言链。 |
| 源文件 | 同时支持 `.csv` 与 `.xlsx`,表头由语言配置驱动。 |
| 编辑器工作流 | `Tools/Localization` 菜单可批量转换变更的或全部源文件。 |
| 资产 Inspector | `LanguageDataSO` 自定义 Inspector 支持导入、导出、搜索、新增、删除,以及重复 Key 和非法内容校验。 |
| 模板 | `<Namespace\|KEY>` 与 `<Namespace\|KEY(arg0,arg1)>` 语法,运行时嵌套解析。 |
| 运行时加载 | 语言数据通过 Addressables 加载,首次加载后缓存。 |

## 安装

需要 **Unity 2022.3** 或更高版本。`com.unity.addressables` 1.22.3 会自动解析依赖。

### 方式一 — Git URL(推荐)

在 Package Manager 中点击 **Add package from git URL**(`+` 按钮),粘贴:

```
https://github.com/Avlorayne/Localization.git
```

或直接在 `Packages/manifest.json` 中添加:

```json
{
  "dependencies": {
    "com.dotline.localization": "https://github.com/Avlorayne/Localization.git"
  }
}
```

### 方式二 — 内嵌包

将本目录复制到目标项目的 `Packages/com.dotline.localization`,Unity 会自动识别,无需在 manifest 中声明。如需显式引用:

```json
{
  "dependencies": {
    "com.dotline.localization": "file:com.dotline.localization"
  }
}
```

## 快速开始

1. 打开 **Tools → Localization → Open Language Config**,创建或选中 `Assets/Settings/LanguageConfig.asset`。
2. 设置 `sourceFolderPath`(默认:`Assets/Editor/Text Files/Localization`)。
3. 设置 `soFolderPath`(默认:`Assets/Resources/Localization`)。运行时加载地址兼容 Resources 风格路径,但生成的资产需标记为 Addressable 才能被当前加载器解析。
4. 在源目录创建 `.csv` 或 `.xlsx` 文件,第一行建议使用 `Key,zh-Hans,zh-Hant,en,ja,ko,Comment`。
5. 执行 **Tools → Localization → Convert Changed Source Files**。
6. 将生成的 `LanguageDataSO` 加入 Addressables,并确认地址可按命名空间或资产路径解析。
7. 在代码中创建 `LocalizationSystem`:

```csharp
using Localization;
using UnityEngine;

public sealed class LocalizedLabelExample : MonoBehaviour
{
    [SerializeField] private LanguageConfigSO languageConfig;

    private LocalizationSystem localization;

    private void Awake()
    {
        localization = new LocalizationSystem(languageConfig);
        localization.SetLanguage("en");
        Debug.Log(localization.GetLocalizedText("<UI|START_GAME>"));
    }
}
```

## 模板语法

| 语法 | 含义 |
| --- | --- |
| `<UI\|START_GAME>` | 基本引用。 |
| `<UI\|ITEM_COUNT(3)>` | 带参数引用。 |
| `<UI\|WELCOME(<UI\|PLAYER_NAME>)>` | 嵌套模板作为参数。 |
| `<UI\|WELCOME(<UI\|PLAYER_NAME>, "Captain")>` | 文本参数必须用双引号包裹,解析后不包含双引号。 |

- 被引用条目中的 `{0}`、`{1}` 会被参数替换,例如:`欢迎你,{0} {1}!` → `欢迎你,队长 Captain!`。
- 注入 `string` 变量同样有效,但也需要双引号包裹:

  ```csharp
  string name = "William";
  localization.GetLocalizedText("<UI|WELCOME(<UI|PLAYER_NAME>, \"{William}\")>");
  ```

- 普通 TextMesh Pro 富文本标签(如 `<color=red>`)不会被当作本地化 Key。

## 文档

| 文档 | 内容 |
| --- | --- |
| [Configuration.md](Documentation~/Configuration.md) | 配置项、源文件格式、路径规则。 |
| [Architecture.md](Documentation~/Architecture.md) | Runtime / Editor 架构与数据流。 |
| [Testing.md](Documentation~/Testing.md) | 自动化测试与人工验收用例。 |
| [FAQ.md](Documentation~/FAQ.md) | 常见问题。 |

## 示例

在 Package Manager 的包详情页 Samples 区域导入 **Basic Localization Example**。导入后会得到一个最小 CSV 源文件和 `BasicLocalizationExample` 脚本,演示配置、转换与运行时查询。

## 已知限制

- WebGL 平台不能同步等待 Addressables 加载,请使用异步加载路径。
- `LocalizationSystem.GetLocalizedText` 依赖同步查询,不适合 WebGL 首次加载时直接查询未缓存资源。
- 源文件被删除时,转换器只清理哈希记录并保留对应 `LanguageDataSO`,避免误删人工维护的数据。
- 包内嵌了第三方二进制依赖,重新分发时请保留 `Third Party Notices.md`。

## 许可证

MIT 许可证,详见 [LICENSE.md](LICENSE.md)。第三方库的许可见 [Third Party Notices.md](Third%20Party%20Notices.md)。
