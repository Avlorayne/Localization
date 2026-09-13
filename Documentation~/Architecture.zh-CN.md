# 技术架构说明

**简体中文** | [English](Architecture.md)

## 总览

本包分为 Runtime 与 Editor 两层：

- Runtime 提供数据结构、模板解析、语言切换和 Addressables 加载。
- Editor 提供源文件导入导出、Inspector 编辑体验、批量转换和数据校验。
- Editor 实现类型统一为 `internal`；独立的 EditMode 测试程序集通过友元程序集声明访问，不扩大包的公开 API。

核心数据流：

```text
CSV/XLSX source
  -> LocalizationSourceParser
  -> LocalizationData[]
  -> LanguageDataSO
  -> Addressables
  -> LocalizationSystem.GetLocalizedText(template)
```

## Assembly 边界

| Assembly | 路径 | 职责 | 依赖 |
| --- | --- | --- | --- |
| `Dotline.Localization` | `Runtime/Dotline.Localization.asmdef` | Runtime API、模板解析、语言数据、Addressables 加载。 | `Unity.Addressables`, `Unity.ResourceManager` |
| `Dotline.Localization.Editor` | `Editor/Dotline.Localization.Editor.asmdef` | 菜单、Inspector、CSV/XLSX 导入导出、校验。 | `Dotline.Localization`, UnityEditor |
| `Dotline.Localization.Editor.Tests` | `Tests/Editor/Dotline.Localization.Editor.Tests.asmdef` | EditMode 自动化测试。 | `Dotline.Localization`, `Dotline.Localization.Editor`, Unity Test Framework |

## Runtime 模块

| 模块 | 说明 |
| --- | --- |
| `LanguageConfigSO` | 项目语言配置与 fallback 定义。 |
| `LanguageDataSO` | 命名空间化语言表，保存 `LocalizationData` 列表。 |
| `LocalizationData` | 单个 Key 的多语言文本和注释。 |
| `LocalizationTemplateParser` | 基于 Superpower 解析 `<Namespace|KEY(args)>` 占位符。 |
| `LocalizationTemplateResolver` | 递归解析占位符，并把参数应用到 `{0}`、`{1}`。 |
| `LocalizationLookup` | 按命名空间懒加载并缓存 `LanguageDataSO`，大小写不敏感查询。 |
| `LanguageDataLoader` | Addressables 同步/异步加载与 handle 缓存。 |
| `LocalizationKeyUtility` | Key 命名规则校验与规范化（大写蛇形命名、括号备注裁剪、显示型 Key 判断）。 |

## Editor 模块

按子目录分组：

| 模块 | 路径 | 说明 |
| --- | --- | --- |
| `LocalizationCSVConverter` | `Editor/Core` | 扫描源目录，按“源文件哈希 + 资产哈希”增量转换 CSV/XLSX 到 SO。 |
| `LanguageConfigSOEditor` | `Editor/Core` | `LanguageConfigSO` 自定义 Inspector。 |
| `LanguageDataSOAddressableRegistrar` | `Editor/Core` | 将通过校验的 `LanguageDataSO` 自动注册到 `Localization` Addressables 分组，地址为 `NamespaceId`。 |
| `LocalizationEditorLanguage` | `Editor/Core` | 探测 Unity 编辑器界面语言并映射到本地化语言 code（带缓存）。 |
| `LocalizationEditorText` | `Editor/Core` | 编辑器 UI 的多语言文案表（en/zh-Hans/zh-Hant/ja/ko）。 |
| `LanguageDataSOEditor` | `Editor/Inspector` | UI Toolkit Inspector，可编辑、导入、导出、筛选和高亮错误。 |
| `LanguageDataSODuplicateKeyValidator` | `Editor/Inspector` | 扫描同一命名空间下的重复 Key。 |
| `LanguageDataSOPathService` | `Editor/Inspector` | 源文件路径解析与规整：项目内路径存 `Assets/...`，外部路径存绝对路径。 |
| `LocalizationSourceParser` | `Editor/Source` | 解析 CSV/XLSX 源文件为 `LocalizationData[]`。 |
| `LocalizationSourceSchema` | `Editor/Source` | 标准表头、语言列映射、字段读写、内容校验。 |
| `LocalizationSourceHeaders` | `Editor/Source` | 源文件表头的语义判断（Key/语言/注释列别名）。 |
| `LocalizationSourceExporter` | `Editor/Source` | 将 `LocalizationData` 列表导出回 CSV/XLSX 源文件。 |
| `CsvParser` | `Editor/Source` | 字符级 CSV 解析与导出，支持引号、逗号、多行字段、UTF-8 BOM 和 GB18030 fallback。 |
| `XlsxLocalizationParser` | `Editor/Source` | 使用 ExcelDataReader 读取 XLSX，多 sheet 导入。 |
| `XlsxLocalizationExporter` | `Editor/Source` | 基于 OpenXML 更新现有工作簿或创建新工作簿。 |
| `OpenXmlWorkbookBuilder` | `Editor/Source` | XLSX 导出用的 OpenXML ZIP 结构构建。 |
| `LocalizationSourceFileAccess` | `Editor/Source` | 以共享读方式读取源文件，避免与 Excel 等进程争抢文件锁。 |

## 模板解析

模板占位符必须显式声明命名空间：

```text
<UI|START_GAME>
<UI|WELCOME(<UI|PLAYER_NAME>)>
<UI|WELCOME(<UI|PLAYER_NAME>, "Playing")>
```

解析器支持括号和尖括号的平衡匹配，所以参数中可以继续嵌套本地化模板。文本参数必须使用双引号，解析后会移除外层双引号；裸参数仅支持数值。解析失败的片段会保留为普通文本，这样 TMP rich text 和用户文本不会被破坏。

## 数据一致性策略

- 源文件导入后会规范化语言列，确保每条数据包含当前配置中的语言代码。
- 同一命名空间内重复 Key 会报错，并以后加载的数据覆盖先前数据。
- 源内容内如果包含 `<Namespace|KEY>` 形式占位符，会被视为内容错误，避免翻译结果再次间接引用 Key。
- 删除源文件时只清理哈希记录，不自动删除 SO。

## 地址与缓存

Runtime 使用 Addressables 加载 `LanguageDataSO`，并维护：

- `ResolvedAddresses`：请求名到实际 Addressables 地址的解析缓存。
- `CachedHandles`：Addressables handle 缓存。
- `CachedResources`：已加载 SO 缓存。

缓存释放方法目前是 internal，不属于对外公开 API。如果未来需要由调用方显式控制缓存生命周期，建议在 `LocalizationSystem` 或专门的 runtime facade 上暴露。

## 工程环境

- Unity: `2022.3.62f3c1`
- Test Framework: `com.unity.test-framework` `1.1.33`
