# 测试用例

**简体中文** | [English](Testing.md)

## 自动化测试

EditMode 测试位于 `Tests/Editor`，测试程序集为 `Dotline.Localization.Editor.Tests`。

运行方式：

1. 将本包放入宿主工程（内嵌到 `Packages/` 或通过 Git URL 引用）。
2. 打开 Unity Test Runner（Window → General → Test Runner）。
3. 选择 EditMode，运行 `Dotline.Localization.Editor.Tests`。

## 已覆盖用例

| 用例 | 目标 |
| --- | --- |
| `LocalizationKeyUtilityTests` | 校验 Key 命名规则、括号备注裁剪、显示型 Key 判断。 |
| `LocalizationTemplateParserTests` | 校验基础占位符、嵌套参数、TMP 标签忽略、嵌套 Key 检测。 |
| `LocalizationDataTests` | 校验语言文本命中和空文本失败。 |
| `LanguageConfigSOTests` | 校验语言查找、一级 fallback 和默认语言回退。 |
| `LocalizationSourceHeadersTests` | 校验注释列表头别名。 |

## 人工验收用例

| 编号 | 场景 | 步骤 | 预期 |
| --- | --- | --- | --- |
| M01 | 初次配置 | 执行 `Tools/Localization/Open Language Config` | 创建并选中 `Assets/Resources/Localization/LanguageConfig.asset`。 |
| M02 | CSV 增量导入 | 修改源目录中的 CSV，执行 `Convert Changed Source Files` | 只更新变更源对应的 `LanguageDataSO`，Console 输出处理数量。 |
| M03 | XLSX 导入 | 准备含 `Key` 表头的 XLSX，执行转换 | 多语言列导入到 `entries.texts`。 |
| M04 | 无表头导入 | 第一个 sheet 第一列使用合法 Key，无 `Key` 表头 | 第一个 sheet 按标准表头导入，其余无 `Key` 表头 sheet 被跳过。 |
| M05 | 重复 Key | 在同命名空间两个 SO 中创建同名 Key，执行校验 | Console 报致命错误并列出冲突文件。 |
| M06 | 非法 Key | 在 Inspector 中输入小写或包含空格的 Key | Inspector 高亮并输出非法 Key 错误。 |
| M07 | 内容误填 Key | 在语言文本中输入 `<UI|START_GAME>` | Inspector 高亮内容字段并输出致命错误。 |
| M08 | 导出 CSV | 在 `LanguageDataSO` Inspector 点击导出，选择 `.csv` | 文件以 UTF-8 BOM 写出，并包含标准表头。 |
| M09 | 导出 XLSX | 在 `LanguageDataSO` Inspector 点击导出，选择现有 `.xlsx` | 保留工作簿结构，更新匹配 Key，并追加缺失 Key。 |
| M10 | 自动 Addressables 注册 | 导入或在 Inspector 中修正 DataSO，直到不再有 Key、重复 Key 或内容占位符错误 | 自动创建/复用 `Localization` 分组；DataSO 以 `NamespaceId` 为地址加入该分组。 |
| M11 | 运行时查询 | 调用 `<UI|START_GAME>` | 返回当前语言文本，缺失语言尝试 fallback。 |
| M12 | 嵌套模板 | 调用 `<UI|WELCOME(<UI|PLAYER_NAME>)>` | 先解析参数，再替换子模板 `{0}`。 |
| M13 | WebGL 同步限制 | 在 WebGL Player 首次同步查询未缓存命名空间 | 文档明确说明该限制；当前公开 API 没有异步/预加载补救入口。 |
| M14 | 默认语言回退 | 配置 `fr -> en`，默认语言为 `zh-Hans`，并让 `fr`、`en` 都缺少该 Key，再用 `fr` 查询 | 返回 `zh-Hans` 文本；不会继续递归 `en` 的 fallback。 |

## 发布前检查

- 所有 EditMode 测试通过。
- Package Manager 能正确显示 README、Samples 和依赖。
- 导入 `Basic Localization Example` 后脚本无编译错误。
- `Third Party Notices.md` 覆盖包内嵌的 ExcelDataReader；Superpower 的声明位于 `com.dotline.superpower` 包。
- `package.json` 版本与 `CHANGELOG.md` 一致。
