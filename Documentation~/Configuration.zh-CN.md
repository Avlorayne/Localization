# 配置/参数说明

**简体中文** | [English](Configuration.md)

## Package 参数

| 字段 | 当前值 | 说明 |
| --- | --- | --- |
| `name` | `com.dotline.localization` | UPM 包名。发布后不应随意修改。 |
| `displayName` | `Localization` | Package Manager 中显示的名称。 |
| `version` | `1.0.4` | 语义化版本。公共 API 或数据格式变更时同步更新。 |
| `license` / `type` | `MIT` / `tool` | 许可证与包类型。 |
| `unity` | `2022.3` | 最低 Unity 版本。 |
| `dependencies.com.unity.addressables` | `1.22.3` | Runtime 通过 Addressables 加载 `LanguageDataSO`。 |
| `dependencies.com.unity.textmeshpro` | `3.0.7` | 为编辑器集成和示例提供 TMP 组件与 Essentials。 |
| `samples` | `Basic Localization Example` | Package Manager 可导入示例。 |

## 语言配置

创建入口：`Tools/Localization/Open Language Config`，会打开 **Project Settings > Localization**。

配置源保存在 `ProjectSettings/DotlineLocalizationSettings.asset`，不放在 `Assets` 下，避免被资源清理误删。`LocalizationSystem` 会自动从 `Resources` 加载烘焙后的运行时 `LanguageConfigSO`，路径为 `Assets/Resources/Localization/LanguageConfig.asset`，运行时加载名为 `Localization/LanguageConfig`。

| 字段 | 默认值 | 说明 |
| --- | --- | --- |
| `sourceFolderPath` | `Assets/Editor/Text Files/Localization` | CSV/XLSX 源文件目录，使用项目相对路径。转换器会递归扫描支持的源文件。 |
| `soFolderPath` | `Assets/Resources/Localization` | 生成 `LanguageDataSO` 的目录。建议放在 Resources 子目录下，并将生成资产配置为 Addressable。 |
| `defaultLanguage` | `zh-Hans` | 主默认语言。`LocalizationData.TryGet` 会优先记录默认语言文本作为兜底值。 |
| `languages` | `zh-Hans`, `zh-Hant`, `en`, `ja`, `ko` | 语言列定义。导入、导出、Inspector 输入框和标准表头都由该列表驱动。 |

在 Project Settings 页面修改配置会自动保存并烘焙运行时 `LanguageConfig.asset`。也可以手动执行 `Tools/Localization/Bake Runtime Language Config`。

## LanguageDefinition

| 字段 | 说明 |
| --- | --- |
| `code` | 语言代码。建议使用 BCP 47 风格，如 `zh-Hans`、`en`、`ja`。 |
| `displayName` | Inspector 显示名；为空时会根据常见语言代码推断。 |
| `fallbackLanguage` | 当前语言缺失时尝试的回退语言代码。 |

## LanguageDataSO

创建入口：`Create > Localization > Language Data`，通常由转换器自动生成。

| 字段 | 说明 |
| --- | --- |
| `sourceFilePath` | 绑定的源文件路径。项目内文件保存为 `Assets/...`，项目外文件保存为绝对路径。 |
| `namespaceId` | 可选命名空间。为空时使用资产名。模板中的 `Namespace` 必须匹配它。 |
| `entries` | 本地化条目列表，每条包含 `key`、多语言 `texts` 和 `comment`。 |
| `legacyCsvFile` | 旧版 CSV 引用迁移字段，Inspector 会尝试转成 `sourceFilePath`。 |

## 源文件格式

推荐表头：

```csv
Key,zh-Hans,zh-Hant,en,ja,ko,Comment
START_GAME,开始游戏,開始遊戲,Start Game,ゲーム開始,게임 시작,主菜单按钮
WELCOME,欢迎 {0},歡迎 {0},Welcome {0},{0} ようこそ,{0} 환영합니다,带参数文本
```

规则：

- `Key` 列大小写不敏感；没有表头时，第一个 sheet 或 CSV 第一列按 `Key` 处理。
- Key 推荐使用 `A-Z`、`0-9`、`_`，例如 `START_GAME`。
- 有表头文件允许显示型 Key，但运行时和团队协作仍建议统一使用大写蛇形命名。
- 语言列可写语言代码、显示名或兼容写法，如 `zh-Hans`、`简体中文`。
- 注释列可写 `Comment`、`Comments`、`Note`、`备注`、`注释` 等。
- 文本内容中不允许存放本地化占位符；编辑器会把这种内容视为致命错误。

## 菜单命令

| 菜单 | 行为 |
| --- | --- |
| `Tools/Localization/Open Language Config` | 打开 Project Settings 中的语言配置。 |
| `Tools/Localization/Bake Runtime Language Config` | 将 Project Settings 配置烘焙到 `Assets/Resources/Localization/LanguageConfig.asset`。 |
| `Tools/Localization/Convert Changed Source Files` | 根据源文件和 SO 哈希，只转换变化项。 |
| `Tools/Localization/Convert All Source Files` | 强制转换全部 `.csv` 和 `.xlsx` 源文件。 |
| `Tools/Localization/Validate Duplicate Keys` | 扫描所有 `LanguageDataSO`，按命名空间检查重复 Key。 |

## 运行时地址解析

通过校验的 `LanguageDataSO` 会由编辑器工具自动注册到 Addressables 的 `Localization` 分组，地址为其 `NamespaceId`（为空时用资产名），无需手动添加。

运行时地址解析会尝试以下 Addressables 地址：

- 输入包含 `/` 或 `\`：原始规范化路径；若不是 `Assets/...`，还会尝试 `Assets/Resources/{path}.asset`。
- 输入不含路径分隔符：`name`、`Localization/name`、`Assets/Resources/Localization/name.asset`。

因此，最稳妥的做法是让 `LanguageDataSO.NamespaceId`、资产名和 Addressables 地址保持一致。

## WebGL 注意事项

WebGL Player 不支持同步 `WaitForCompletion`。当前公开 API 只有同步取词，所以本版本不支持 WebGL 上首次查询未缓存命名空间。该场景需要未来提供公开的异步/预加载 API。
