# FAQ

**简体中文** | [English](FAQ.md)

## 为什么生成目录默认放在 `Assets/Resources/Localization`？

历史默认值和地址候选都兼容 Resources 风格路径。当前运行时实际通过 Addressables 加载资产；当 `LanguageDataSO` 通过 Key、重复 Key 和内容占位符校验后，工具会自动将其加入 `Localization` Addressables 分组，并将地址设为其 `NamespaceId`。

`LanguageConfigSO` 也由包菜单默认创建在 `Assets/Resources/Localization`，因为它是运行时配置而不是仅 Editor 使用的状态。源文件哈希缓存仍放在 `Assets/Settings`，因为它只属于 Editor 数据。

## 为什么运行时提示找不到 Addressable language data？

常见原因是数据未通过合法性校验，或 Addressables 地址和命名空间不一致。自动注册使用 `NamespaceId` 作为地址；仍建议让 `LanguageDataSO` 资产名和 `NamespaceId` 保持一致，例如都使用 `UI`。

## CSV 乱码怎么办？

导入器会优先识别 BOM，无 BOM 时严格尝试 UTF-8，失败后使用 GB18030 作为中文 Excel ANSI 文件的 fallback。团队协作仍建议统一保存为 UTF-8。

## Key 可以写中文或小写吗？

不建议。运行时 Key 最稳妥的格式是 `A-Z`、`0-9`、`_`，例如 `MAIN_MENU_START`。有表头源文件可以导入显示型 Key，但 Inspector 会把非法 Key 标成错误。

## 文本里能不能写 `<UI|SOME_KEY>`？

不要把本地化 Key 存在语言文本内容中。工具会把语言内容里的本地化占位符视为错误，避免翻译数据形成隐式依赖链。需要组合文本时，在调用模板里传入嵌套参数。

## 模板参数什么时候要加引号？

裸参数（不带引号）仅支持数值，如 `<UI|ITEM_COUNT(3)>`；文本参数必须用英文双引号包裹，如 `<UI|WELCOME(<UI|PLAYER_NAME>, "Captain")>`，解析后会移除外层双引号。引号内的内容原样传递，不会做变量替换——动态值请在代码里用字符串插值拼进模板再传入。

## 如何新增语言？

在 `LanguageConfigSO.languages` 中添加 `LanguageDefinition`，例如 `fr`。之后重新导入或导出源文件，标准表头和 Inspector 字段会自动包含新语言列。

## 为什么删除源文件后 SO 还在？

转换器只会清理哈希记录，不会自动删除 `LanguageDataSO`。这是为了避免误删已经手工维护或仍在版本中的语言数据。

## WebGL 为什么不能同步加载？

当前公开 API 只有同步取词。WebGL 平台不支持 Addressables 的同步等待，所以本版本不支持首次查询未缓存命名空间。该场景需要公开的异步/预加载 API。

## 能和 Unity 官方 Localization 包共存吗？

可以共存，但两者职责不同。本包使用自定义 `LanguageDataSO`、源文件转换器和模板语法；不要假设它会读取 Unity 官方 String Table。
