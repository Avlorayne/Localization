# 本地化系统报错与警告条例

本文集中整理 `com.dotline.localization` 系统中的错误、警告、弹窗与前端 Editor 建议处理策略，供前端 Editor 编写、QA 验收、策划排查和运行时调试使用。

## 严重级别

| 级别 | 含义 | 建议处理 |
| --- | --- | --- |
| 致命错误 | 数据非法或关键流程失败。修复前不应信任生成的本地化结果。 | 尽量阻止保存、发布、Addressables 注册，并在 Editor UI 中明确展示。 |
| 错误 | Runtime 或 Editor 操作失败。旧数据可能仍保留，但本次请求没有成功完成。 | 展示失败状态，并给出可执行的修复建议。 |
| 警告 | 系统已恢复、跳过了可选步骤，或检测到高风险配置。 | 不阻断流程，但需要让用户能看到。 |
| 弹窗 | Unity Editor 中向用户展示的阻塞式确认或错误提示。 | 前端 Editor 应把它建模为一次操作结果。 |

## 通用数据规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-DATA-001 | 致命错误 | `LanguageDataSO` 内的 Key 必须是合法字面量。 | Key 为空，或包含 `A-Z`、`0-9`、`_` 之外的字符。 | 重命名 Key，建议使用大写蛇形命名，例如 `START_GAME`。 | `LanguageDataSOEditor`, `LanguageDataSOAddressableRegistrar` |
| LOC-DATA-002 | 致命错误 | 同一命名空间内 Key 必须唯一。 | 两个或多个 `LanguageDataSO` 使用相同有效命名空间，并包含相同 Key。 | 删除或重命名重复条目。同一命名空间只能有一个同名 Key。 | `LanguageDataSODuplicateKeyValidator`, `LocalizationLookup` |
| LOC-DATA-003 | 致命错误 | 语言文本内容里不允许存放本地化占位符。 | 任一语言文本字段中出现可解析的占位符，例如 `<UI\|START_GAME>`。 | 不要在翻译文本里引用 Key；需要组合文本时，在调用方模板中传入嵌套参数。 | `LocalizationSourceSchema`, `LanguageDataSOEditor` |
| LOC-DATA-004 | 警告 | 空 Key 条目会在运行时被忽略。 | 加载的 `LanguageDataSO` 中存在空 Key 条目。 | 在 Inspector 或源表中修复或删除该条目。 | `LocalizationLookup` |
| LOC-DATA-005 | 警告 | `LanguageDataSO` 的命名空间应保持唯一。 | 显式 `namespaceId` 为空时，系统回退使用资产名；该资产名已被另一个 SO 缓存。 | 设置唯一 `namespaceId`，或重命名资产。 | `LanguageDataSO` |

## LanguageConfig 配置规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-CONFIG-001 | 错误 | 转换前必须配置源文件目录和 SO 输出目录。 | `sourceFolderPath` 或 `soFolderPath` 为空。 | 打开 `Tools/Localization/Open Language Config`，填写两个路径。 | `LocalizationSourceConverter` |
| LOC-CONFIG-002 | 错误 | 转换前源文件目录必须存在。 | `sourceFolderPath` 指向不存在的文件夹。 | 创建该目录，或修正路径。 | `LocalizationSourceConverter` |
| LOC-CONFIG-003 | 警告 | 生成的 SO 目录建议位于 `Resources` 目录下。 | `soFolderPath` 不包含 `/Resources/`，且不以 `Resources/` 开头。 | 默认建议使用 `Assets/Resources/Localization`，除非项目有自定义加载路径。 | `LanguageConfigSOEditor` |
| LOC-CONFIG-004 | 警告 | 未知语言代码会回退到默认语言。 | `LanguageConfigSO.GetDefinition(languageCode)` 找不到请求的语言代码。 | 在 **Project Settings → Localization** 中加入该语言，或请求已有语言。 | `LanguageConfigSO` |
| LOC-CONFIG-005 | 警告 | 设置当前语言为未知代码时会被拒绝。 | 给 `LocalizationSystem.CurrentLanguageCode` 赋值时，该代码不存在于已配置语言列表。 | 使用已配置的语言代码。 | `LocalizationSystem` |
| LOC-CONFIG-006 | 警告 | 缺少语言配置时，Editor 解析/导出会使用内置语言列。 | Project Settings 语言定义不可用。 | 导入或导出前打开 **Project Settings → Localization**。 | `LocalizationSourceSchema` |
| LOC-CONFIG-007 | 警告 | 配置中的重复语言代码会被忽略。 | 两个 `LanguageDefinition` 归一化后得到相同语言代码。 | 每种语言代码只保留一个定义。 | `LocalizationSourceSchema` |
| LOC-CONFIG-008 | 警告 | 运行时语言配置可能过期。 | Project Settings 已变化，但烘焙到 `Resources/Localization/LanguageConfig` 的副本未刷新。 | 执行 `Tools/Localization/Bake Runtime Language Config`。 | `LanguageConfigBaker` |

## 源文件导入与转换规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-SOURCE-001 | 警告 | 无法读取的源文件或 SO 会跳过本次转换。 | 计算哈希时抛出 `IOException` 或 `UnauthorizedAccessException`。 | 关闭 Excel 或其他占用文件的进程，然后重新转换。 | `LocalizationSourceConverter` |
| LOC-SOURCE-002 | 警告 | 空源文件不会更新已有 SO 数据。 | 源文件解析结果为 0 个本地化条目。 | 检查源文件是否有 `Key` 列，或无表头行是否符合规则。 | `LocalizationSourceConverter` |
| LOC-SOURCE-003 | 警告 | 源文件删除后只清理哈希记录，不自动删除 SO。 | 之前记录过哈希的源文件已不存在。 | 人工检查对应 SO。系统故意保留它，避免误删手工维护数据。 | `LocalizationSourceConverter` |
| LOC-SOURCE-004 | 警告 | 哈希缓存损坏时会被忽略。 | 读取 `Assets/Settings/LocalizationSourceHashes.json` 失败。 | 再次执行转换；成功后缓存会重新生成。 | `LocalizationSourceConverter` |
| LOC-SOURCE-005 | 错误 | 不支持的源文件格式不能导入。 | 源文件扩展名不是 `.csv` 或 `.xlsx`。 | 使用 `.csv` 或 `.xlsx`。 | `LocalizationSourceParser`, `LanguageDataSOEditor` |
| LOC-SOURCE-006 | 警告 | 未知源文件列会被跳过。 | 表头不是 `Key`、已配置语言列或注释列别名。 | 修正表头拼写，或在语言配置中加入对应语言。 | `LocalizationSourceSchema` |
| LOC-SOURCE-007 | 弹窗 | 导入前必须指定源文件路径。 | 在 SO 的 `sourceFilePath` 为空时点击 `Import from Source`。 | 先指定源文件。 | `LanguageDataSOEditor` |
| LOC-SOURCE-008 | 弹窗 | 导入源文件必须存在。 | 存储的源文件路径解析后找不到文件。 | 恢复源文件，或更新 `sourceFilePath`。 | `LanguageDataSOEditor` |
| LOC-SOURCE-009 | 弹窗 | 空导入结果不会覆盖 SO。 | 从源文件导入时返回 0 个条目。 | 修复源文件结构后重试。 | `LanguageDataSOEditor` |
| LOC-SOURCE-010 | 弹窗 | 导入会替换目标 SO 中的全部条目。 | 用户将受支持源文件导入到已有 SO。 | 需要显式确认；前端 Editor 应将它展示为覆盖性操作。 | `LanguageDataSOEditor` |

## CSV 规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-CSV-001 | 错误 | CSV 文件必须存在。 | `CsvParser.Import(csvPath)` 收到不存在的路径。 | 恢复 CSV，或修正 `sourceFilePath`。 | `CsvParser` |
| LOC-CSV-002 | 警告 | 非 UTF-8 CSV 的回退读取依赖 GB18030 编码支持。 | CSV 无 BOM，严格 UTF-8 解码失败，且 `Encoding.GetEncoding("GB18030")` 不可用。 | 如果中文字符异常，另存为 UTF-8 with BOM。 | `CsvParser` |
| LOC-CSV-003 | 警告 | 非法 CSV 行会被静默跳过。 | 某行 Key 无法被 `LocalizationKeyUtility` 规范化。 | 检查 Key 为空或非法的行。 | `CsvParser` |

## XLSX 规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-XLSX-001 | 错误 | XLSX 文件必须存在。 | `XlsxLocalizationParser.Import(xlsxPath)` 收到不存在的路径。 | 恢复 XLSX，或修正 `sourceFilePath`。 | `XlsxLocalizationParser` |
| LOC-XLSX-002 | 错误 | XLSX 导入失败会附带异常信息。 | ExcelDataReader 无法打开或读取工作簿。 | 关闭 Excel，确认文件是有效 `.xlsx`，然后重试。 | `XlsxLocalizationParser` |
| LOC-XLSX-003 | 警告 | 没有 `Key` 表头的工作表会被跳过，首个工作表的无表头导入除外。 | 非首个 sheet 缺少 `Key`，或首个 sheet 不像本地化数据。 | 给每个要导入的工作表添加 `Key` 表头。 | `XlsxLocalizationParser` |
| LOC-XLSX-004 | 错误 | 导出到已有工作簿时，至少需要一个带 `Key` 表头的工作表。 | 补丁式导出现有 workbook，但没有任何 sheet 包含 `Key` 表头。 | 添加 `Key` 表头，或导出到一个新工作簿。 | `XlsxLocalizationExporter` |
| LOC-XLSX-005 | 错误 | XLSX 导出失败会附带异常信息。 | 创建或修改 workbook 时抛出异常。 | 检查文件权限、工作簿结构，以及文件是否被占用。 | `XlsxLocalizationExporter` |

## 导出规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-EXPORT-001 | 错误 | 不支持的导出格式不能写入。 | 输出文件扩展名不是 `.csv` 或 `.xlsx`。 | 导出为 `.csv` 或 `.xlsx`。 | `LocalizationSourceExporter`, `LanguageDataSOEditor` |
| LOC-EXPORT-002 | 弹窗 | 导出会覆盖或修改目标源文件。 | 用户导出到已有路径或选定的源文件路径。 | 需要显式确认；前端 Editor 应明确展示覆盖目标。 | `LanguageDataSOEditor` |
| LOC-EXPORT-003 | 弹窗 | 导出失败会提示用户。 | 导出方法返回 `false`。 | 展示目标路径，并引导用户查看 Console 中的底层错误。 | `LanguageDataSOEditor` |

## Addressables 规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-ADDR-001 | 警告 | 自动注册必须能够获取或创建 Addressables Settings。 | `AddressableAssetSettingsDefaultObject.GetSettings(true)` 返回 null。 | 检查 Addressables 包、项目资产权限和 Settings 完整性。 | `LanguageDataSOAddressableRegistrar` |
| LOC-ADDR-002 | 错误 | 必须能创建或找到 `Localization` Addressables 分组。 | 创建 `Localization` 分组返回 null。 | 检查 Addressables 包和 Settings 是否损坏。 | `LanguageDataSOAddressableRegistrar` |
| LOC-ADDR-003 | 错误 | 必须能把资产条目创建或移动到 `Localization` 分组。 | `CreateOrMoveEntry` 返回 null。 | 检查资产 GUID、Addressables Settings 和包状态。 | `LanguageDataSOAddressableRegistrar` |
| LOC-ADDR-004 | 规则 | 非法本地化数据不会注册到 Addressables。 | Key、重复 Key 或文本内嵌占位符校验失败。 | 先修复校验错误；数据干净后才会注册。 | `LanguageDataSOAddressableRegistrar`, `LanguageDataSOEditor` |

## 运行时加载与查找规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-RUNTIME-001 | 错误 | Addressable 语言数据地址不能为空。 | Runtime 加载收到 null、空字符串或全空白地址。 | 检查模板命名空间和加载器输入。 | `LanguageDataLoader` |
| LOC-RUNTIME-002 | 错误 | WebGL Player 不支持同步 Addressables 加载。 | 非 Editor 的 WebGL 环境调用 `TryLoadDataResource`。 | WebGL 上应预加载，或先暴露公开异步 API。 | `LanguageDataLoader` |
| LOC-RUNTIME-003 | 错误 | Addressables 地址解析失败。 | 所有候选地址都无法解析到 `LanguageDataSO`。 | 确认 SO 已注册为 Addressable，且地址等于 `NamespaceId`。 | `LanguageDataLoader` |
| LOC-RUNTIME-004 | 错误 | Addressables 资产加载失败。 | Load 操作失败或抛异常。 | 查看 operation exception；必要时重建 Addressables。 | `LanguageDataLoader` |
| LOC-RUNTIME-005 | 错误 | 模板命名空间不能为空。 | Resolver 请求 lookup 时命名空间为空。 | 模板应使用 `<Namespace\|KEY>` 形式。 | `LocalizationLookup` |
| LOC-RUNTIME-006 | 错误 | 命名空间无法加载。 | Lookup 未命中命名空间，Loader 也无法加载。 | 检查命名空间拼写、资产名、`NamespaceId` 和 Addressables 地址。 | `LocalizationLookup` |
| LOC-RUNTIME-007 | 警告 | 找不到 Key 或对应语言文本。 | 命名空间加载成功，但 Key/语言查找在 fallback 后仍失败。 | 检查 Key 拼写，以及语言、fallback、default language 配置。 | `LocalizationLookup` |
| LOC-RUNTIME-008 | 错误 | 运行时重复 Key 会覆盖先前条目。 | 加载的数据中，同一命名空间存在重复 Key。 | 修复数据；运行时会保留后加载的值。 | `LocalizationLookup` |

## 模板解析规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-TEMPLATE-001 | 规则 | 合法占位符格式为 `<Namespace\|KEY>` 或 `<Namespace\|KEY(args)>`。 | Parser 读到符合语法的尖括号占位符。 | 每个占位符都应显式写出命名空间。 | `LocalizationTemplateParser` |
| LOC-TEMPLATE-002 | 规则 | 非法的类占位符文本会被当作普通文本。 | Parser 遇到非法尖括号内容、TMP 富文本或错误参数格式。 | 不要依赖错误模板自动报错；前端 Editor 应主动做模板校验。 | `LocalizationTemplateParser` |
| LOC-TEMPLATE-003 | 规则 | 参数可以是双引号字符串、数字或嵌套占位符。 | 参数解析逐项校验。 | 字面量字符串必须用英文双引号包住。缺失参数时，`{index}` 会原样保留。 | `LocalizationTemplateParser`, `LocalizationTemplateResolver` |

## Inspector UI 规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-UI-001 | 警告 | USS 缺失只影响 Inspector 样式。 | 无法加载 `LanguageDataSO.uss`。 | 如果 Inspector 显示异常，恢复 USS 文件。 | `LanguageDataSOEditor` |
| LOC-UI-002 | 致命错误 | Inspector 错误框会汇总重复 Key、非法 Key、文本内嵌占位符。 | 三类校验中任意一类存在问题。 | 修复所有展示的问题，再期待 Addressables 自动注册。 | `LanguageDataSOEditor` |
| LOC-UI-003 | 弹窗 | 删除条目需要确认。 | 用户点击某个条目的删除按钮。 | 只有确认该条目应从 SO 中移除时才继续。 | `LanguageDataSOEditor` |

## 示例规则

| ID | 级别 | 条例 | 触发条件 | 处理建议 | 来源 |
| --- | --- | --- | --- | --- | --- |
| LOC-RUNTIME-008 | 错误 | 缺少运行时语言配置。 | `LocalizationSystem` 无法加载 `Resources/Localization/LanguageConfig`。 | 将 Project Settings 烘焙到 `Assets/Resources/Localization/LanguageConfig.asset`。 | `LocalizationSystem` |

## 前端 Editor 处理策略

1. 所有“致命错误”都应作为保存、发布、注册的阻断项。
2. 所有“错误”都应视为操作失败；除非 Unity API 已经实际修改数据，否则前端应保持旧数据不变。
3. 所有“警告”默认不阻断流程，但必须可见。
4. 能拿到上下文时，始终展示资产路径、命名空间、Key、语言代码和源文件路径。
5. 批量转换结果应同时返回已处理文件和已跳过文件。
6. 导入会替换目标 SO 的全部条目，前端必须明确展示这个后果。
7. 导出可能覆盖或 patch 源文件，前端必须明确展示目标路径。
8. 每次编辑或导入成功后，应执行与 Inspector 一致的三类校验：非法 Key、重复 Key、文本内嵌本地化占位符。
9. 只有校验通过的 `LanguageDataSO` 才允许注册到 Addressables。
10. WebGL 场景不要对未缓存命名空间调用同步 Runtime 查找路径。

## 建议给前端 API 使用的稳定错误码

即使 Console 文案以后做本地化或改写，前端 API 也建议使用这些稳定 code：

```text
INVALID_KEY
DUPLICATE_KEY_IN_NAMESPACE
EMBEDDED_LOCALIZATION_PLACEHOLDER
EMPTY_KEY_SKIPPED
NON_UNIQUE_NAMESPACE
MISSING_CONFIG_PATH
SOURCE_FOLDER_MISSING
SO_FOLDER_NOT_RESOURCES
UNKNOWN_LANGUAGE_CODE
MISSING_LANGUAGE_CONFIG
DUPLICATE_LANGUAGE_CODE
MULTIPLE_LANGUAGE_CONFIGS
FILE_READ_FAILED
SOURCE_EMPTY
SOURCE_DELETED_HASH_CLEANED
HASH_CACHE_LOAD_FAILED
UNSUPPORTED_SOURCE_FORMAT
UNKNOWN_SOURCE_COLUMN
SOURCE_FILE_REQUIRED
SOURCE_FILE_NOT_FOUND
CSV_NOT_FOUND
CSV_ENCODING_FALLBACK_UNAVAILABLE
XLSX_NOT_FOUND
XLSX_IMPORT_FAILED
XLSX_SHEET_SKIPPED
XLSX_NO_KEY_SHEET
XLSX_EXPORT_FAILED
UNSUPPORTED_EXPORT_FORMAT
EXPORT_FAILED
ADDRESSABLE_SETTINGS_MISSING
ADDRESSABLE_GROUP_CREATE_FAILED
ADDRESSABLE_ENTRY_CREATE_FAILED
ADDRESSABLE_ADDRESS_EMPTY
WEBGL_SYNC_LOAD_UNSUPPORTED
ADDRESSABLE_ADDRESS_NOT_FOUND
ADDRESSABLE_LOAD_FAILED
EMPTY_NAMESPACE
NAMESPACE_LOAD_FAILED
KEY_OR_LANGUAGE_NOT_FOUND
INSPECTOR_STYLE_MISSING
SAMPLE_CONFIG_MISSING
```
