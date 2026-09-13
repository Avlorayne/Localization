#if UNITY_EDITOR
using System.Collections.Generic;

namespace Localization.Editor
{
    internal static class LocalizationEditorText
    {
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["source.configuration"] = new[] { "Source Configuration", "源文件配置", "來源檔案設定", "ソース設定", "원본 파일 설정" },
            ["language.configuration"] = new[] { "Language Configuration", "语言设置", "語言設定", "言語設定", "언어 설정" },
            ["source.file"] = new[] { "Source File", "源文件", "來源檔案", "ソースファイル", "원본 파일" },
            ["browse"] = new[] { "Browse", "浏览", "瀏覽", "参照", "찾아보기" },
            ["import.source"] = new[] { "Import from Source", "从源文件导入", "從來源檔案匯入", "ソースからインポート", "원본에서 가져오기" },
            ["export.source"] = new[] { "Export to Source", "导出到源文件", "匯出到來源檔案", "ソースへエクスポート", "원본으로 내보내기" },
            ["add.entry"] = new[]
                { "+ Add New Localization Entry", "+ 添加本地化条目", "+ 新增在地化項目", "+ ローカライズ項目を追加", "+ 현지화 항목 추가" },
            ["filter"] = new[] { "Filter", "筛选", "篩選", "フィルター", "필터" },
            ["delete"] = new[] { "Delete", "删除", "刪除", "削除", "삭제" },
            ["key"] = new[] { "Key", "键", "鍵", "キー", "키" },
            ["comment"] = new[] { "Comment", "注释", "註解", "コメント", "주석" },
            ["zh.hans"] = new[]
                { "Simplified Chinese", "简体中文 (zh-Hans)", "簡體中文 (zh-Hans)", "簡体字中国語 (zh-Hans)", "중국어 간체 (zh-Hans)" },
            ["zh.hant"] = new[]
                { "Traditional Chinese", "繁体中文 (zh-Hant)", "繁體中文 (zh-Hant)", "繁体字中国語 (zh-Hant)", "중국어 번체 (zh-Hant)" },
            ["english"] = new[] { "English (en)", "英语 (en)", "英文 (en)", "英語 (en)", "영어 (en)" },
            ["japanese"] = new[] { "Japanese (ja)", "日语 (ja)", "日文 (ja)", "日本語 (ja)", "일본어 (ja)" },
            ["korean"] = new[] { "Korean (ko)", "韩语 (ko)", "韓文 (ko)", "韓国語 (ko)", "한국어 (ko)" },

            ["delete.entry.title"] = new[] { "Delete Entry", "删除条目", "刪除項目", "項目を削除", "항목 삭제" },
            ["delete.entry.message"] = new[]
            {
                "Are you sure you want to delete Key: '{0}'?", "确定要删除键：'{0}' 吗？", "確定要刪除鍵：'{0}' 嗎？",
                "キー '{0}' を削除しますか？", "키 '{0}'를 삭제할까요?"
            },
            ["yes"] = new[] { "Yes", "是", "是", "はい", "예" },
            ["no"] = new[] { "No", "否", "否", "いいえ", "아니요" },
            ["ok"] = new[] { "OK", "确定", "確定", "OK", "확인" },
            ["cancel"] = new[] { "Cancel", "取消", "取消", "キャンセル", "취소" },
            ["import"] = new[] { "Import", "导入", "匯入", "インポート", "가져오기" },
            ["export"] = new[] { "Export", "导出", "匯出", "エクスポート", "내보내기" },
            ["error"] = new[] { "Error", "错误", "錯誤", "エラー", "오류" },

            ["select.source.file"] = new[]
                { "Select Localization Source File", "选择本地化源文件", "選擇在地化來源檔案", "ローカライズソースファイルを選択", "현지화 원본 파일 선택" },
            ["assign.source.first"] = new[]
                { "Assign a source file first.", "请先指定源文件。", "請先指定來源檔案。", "先にソースファイルを指定してください。", "먼저 원본 파일을 지정하세요." },
            ["source.not.exist"] = new[]
            {
                "Source file does not exist:\n{0}", "源文件不存在：\n{0}", "來源檔案不存在：\n{0}", "ソースファイルが存在しません:\n{0}",
                "원본 파일이 없습니다:\n{0}"
            },
            ["unsupported.source"] = new[]
            {
                "Unsupported source file format:\n{0}", "不支持的源文件格式：\n{0}", "不支援的來源檔案格式：\n{0}", "未対応のソースファイル形式です:\n{0}",
                "지원하지 않는 원본 파일 형식입니다:\n{0}"
            },
            ["import.source.title"] = new[] { "Import Source", "导入源文件", "匯入來源檔案", "ソースをインポート", "원본 가져오기" },
            ["import.source.confirm"] = new[]
            {
                "Replace ALL entries in '{0}' with data from:\n{1}", "用以下源文件数据替换 '{0}' 中的所有条目：\n{1}",
                "使用以下來源檔案資料取代 '{0}' 的所有項目：\n{1}", "'{0}' の全項目を次のソースデータで置換します:\n{1}",
                "'{0}'의 모든 항목을 다음 원본 데이터로 교체합니다:\n{1}"
            },
            ["no.entries.imported"] = new[]
            {
                "No localization entries were found. The target SO was not changed.", "没有找到本地化条目，目标 SO 未更改。",
                "找不到在地化項目，目標 SO 未變更。", "ローカライズ項目が見つかりません。対象 SO は変更されませんでした。", "현지화 항목을 찾지 못했습니다. 대상 SO는 변경되지 않았습니다."
            },
            ["export.file.title"] = new[] { "Export File", "导出文件", "匯出檔案", "ファイルをエクスポート", "파일 내보내기" },
            ["unsupported.export"] = new[]
            {
                "Unsupported export file format:\n{0}", "不支持的导出文件格式：\n{0}", "不支援的匯出檔案格式：\n{0}", "未対応のエクスポート形式です:\n{0}",
                "지원하지 않는 내보내기 파일 형식입니다:\n{0}"
            },
            ["export.confirm"] = new[]
            {
                "Overwrite localization source file:\n{0}", "覆盖本地化源文件：\n{0}", "覆寫在地化來源檔案：\n{0}",
                "ローカライズソースファイルを上書きします:\n{0}", "현지화 원본 파일을 덮어씁니다:\n{0}"
            },
            ["export.failed"] = new[]
            {
                "Failed to export file:\n{0}", "导出文件失败：\n{0}", "匯出檔案失敗：\n{0}", "ファイルのエクスポートに失敗しました:\n{0}",
                "파일 내보내기에 실패했습니다:\n{0}"
            },
            ["export.localization.file"] = new[]
                { "Export Localization File", "导出本地化文件", "匯出在地化檔案", "ローカライズファイルをエクスポート", "현지화 파일 내보내기" },

            ["settings.source.folder"] = new[] { "Source Folder Path", "源文件夹路径", "來源資料夾路徑", "ソースフォルダーパス", "원본 폴더 경로" },
            ["settings.source.folder.tooltip"] = new[]
            {
                "Folder containing localization source files (.xlsx or .csv), relative to the project root.",
                "包含本地化源文件（.xlsx 或 .csv）的文件夹，相对于项目根目录。", "包含在地化來源檔案（.xlsx 或 .csv）的資料夾，相對於專案根目錄。",
                "ローカライズソースファイル（.xlsx または .csv）を含むフォルダー。プロジェクトルートからの相対パスです。",
                "현지화 원본 파일(.xlsx 또는 .csv)이 있는 폴더입니다. 프로젝트 루트 기준 상대 경로입니다."
            },
            ["settings.so.folder"] = new[] { "SO Folder Path", "SO 文件夹路径", "SO 資料夾路徑", "SO フォルダーパス", "SO 폴더 경로" },
            ["settings.so.folder.tooltip"] = new[]
            {
                "Folder where generated LanguageDataSO files are saved, relative to the project root.",
                "生成的 LanguageDataSO 文件保存到的文件夹，相对于项目根目录。", "產生的 LanguageDataSO 檔案儲存到的資料夾，相對於專案根目錄。",
                "生成された LanguageDataSO ファイルを保存するフォルダー。プロジェクトルートからの相対パスです。",
                "생성된 LanguageDataSO 파일을 저장할 폴더입니다. 프로젝트 루트 기준 상대 경로입니다."
            },
            ["settings.so.folder.not.resources"] = new[]
            {
                "Runtime loads LanguageDataSO from a Resources folder. Keep the SO folder under a 'Resources' subfolder, otherwise the generated SOs will not load at runtime.",
                "运行时从 Resources 目录加载 LanguageDataSO。SO 目录应位于某个 Resources 子目录下，否则生成的 SO 运行时将无法加载。",
                "執行階段從 Resources 目錄載入 LanguageDataSO。SO 目錄應位於某個 Resources 子目錄下，否則生成的 SO 執行階段將無法載入。",
                "ランタイムは Resources フォルダーから LanguageDataSO を読み込みます。SO フォルダーはいずれかの Resources サブフォルダー配下に置いてください。そうしないと実行時に読み込まれません。",
                "런타임은 Resources 폴더에서 LanguageDataSO를 로드합니다. SO 폴더를 Resources 하위 폴더 아래에 두지 않으면 빌드에서 로드되지 않습니다."
            },
            ["key.namespace"] = new[] { "Namespace", "命名空间", "命名空間", "名前空間", "네임스페이스" },
            ["undo.add.entry"] = new[]
                { "Add New Localization Entry", "添加本地化条目", "新增在地化項目", "ローカライズ項目を追加", "현지화 항목 추가" },
            ["undo.delete.entry"] = new[]
                { "Delete Localization Entry", "删除本地化条目", "刪除在地化項目", "ローカライズ項目を削除", "현지화 항목 삭제" },
            ["undo.import.source"] = new[]
                { "Import Localization Source", "导入本地化源文件", "匯入在地化來源檔案", "ローカライズソースをインポート", "현지화 원본 가져오기" },
            ["undo.edit.entry"] = new[]
                { "Edit Localization Entry", "编辑本地化条目", "編輯在地化項目", "ローカライズ項目を編集", "현지화 항목 편집" },
            ["undo.normalize.invalid.keys"] = new[]
            {
                "Normalize Invalid Localization Keys", "正则化非法本地化 Key", "正規化非法在地化 Key", "不正なローカライズキーを正規化",
                "잘못된 현지화 Key 정규화"
            },
            ["normalize.invalid.keys"] = new[]
            {
                "Normalize Invalid Keys", "一键正则化大写", "一鍵正規化大寫", "不正キーを正規化", "잘못된 Key 정규화"
            },

            ["duplicate.key.error"] = new[]
            {
                "[Fatal Error] Detected {0} duplicate Key(s). Please fix immediately.\nDuplicates: {1}",
                "【致命错误】检测到 {0} 个重复 Key，请立即修正。\n重复项：{1}", "【嚴重錯誤】偵測到 {0} 個重複 Key，請立即修正。\n重複項：{1}",
                "[致命的エラー] {0} 個の重複キーを検出しました。すぐに修正してください。\n重複: {1}", "[치명적 오류] 중복 키 {0}개를 감지했습니다. 즉시 수정하세요.\n중복: {1}"
            },
            ["invalid.key.error"] = new[]
            {
                "[Fatal Error] Detected {0} invalid Key(s). Keys may contain only A-Z, 0-9, and underscore.\nInvalid keys: {1}",
                "【致命错误】检测到 {0} 个非法 Key。Key 只能包含 A-Z、0-9 和下划线。\n非法项：{1}",
                "【嚴重錯誤】偵測到 {0} 個非法 Key。Key 只能包含 A-Z、0-9 與底線。\n非法項：{1}",
                "[致命的エラー] {0} 個の不正なキーを検出しました。キーに使用できるのは A-Z、0-9、アンダースコアのみです。\n不正なキー: {1}",
                "[치명적 오류] 잘못된 Key {0}개를 감지했습니다. Key에는 A-Z, 0-9, 밑줄만 사용할 수 있습니다.\n잘못된 항목: {1}"
            },
            ["embedded.content.key.error"] = new[]
            {
                "[Fatal Error] Detected {0} localization key placeholder(s) inside language content. Language content must not store localization keys.\nInvalid fields: {1}",
                "【致命错误】检测到 {0} 个语言内容中包含本地化 Key。语言内容中不允许存放本地化键。\n问题字段：{1}",
                "【嚴重錯誤】偵測到 {0} 個語言內容中包含在地化 Key。語言內容中不允許存放在地化鍵。\n問題欄位：{1}",
                "[致命的エラー] 言語内容内に {0} 個のローカライズキーを検出しました。言語内容にローカライズキーを保存しないでください。\n問題のフィールド: {1}",
                "[치명적 오류] 언어 내용 안에 현지화 키 {0}개가 감지되었습니다. 언어 내용에는 현지화 키를 저장할 수 없습니다.\n문제 필드: {1}"
            },

            ["log.imported.source"] = new[]
            {
                "[Localization] Imported source into SO '{0}': {1}", "[Localization] 已将源文件导入 SO '{0}'：{1}",
                "[Localization] 已將來源檔案匯入 SO '{0}'：{1}", "[Localization] ソースを SO '{0}' にインポートしました: {1}",
                "[Localization] 원본을 SO '{0}'로 가져왔습니다: {1}"
            },
            ["log.created.settings"] = new[]
            {
                "[Localization] Created new language config at {0}", "[Localization] 已创建新语言配置：{0}",
                "[Localization] 已建立新語言設定：{0}", "[Localization] 新しい言語設定を作成しました: {0}", "[Localization] 새 언어 설정 생성: {0}"
            },
            ["log.missing.settings"] = new[]
            {
                "[Localization] sourceFolderPath or soFolderPath is not set in LanguageConfigSO.",
                "[Localization] LanguageConfigSO 中未设置 sourceFolderPath 或 soFolderPath。",
                "[Localization] LanguageConfigSO 未設定 sourceFolderPath 或 soFolderPath。",
                "[Localization] LanguageConfigSO に sourceFolderPath または soFolderPath が設定されていません。",
                "[Localization] LanguageConfigSO에 sourceFolderPath 또는 soFolderPath가 설정되지 않았습니다."
            },
            ["log.source.folder.missing"] = new[]
            {
                "[Localization] Source folder does not exist: {0}", "[Localization] 源文件夹不存在：{0}",
                "[Localization] 來源資料夾不存在：{0}", "[Localization] ソースフォルダーが存在しません: {0}", "[Localization] 원본 폴더가 없습니다: {0}"
            },
            ["log.conversion.complete"] = new[]
            {
                "[Localization] Conversion complete. Processed {0} files.", "[Localization] 转换完成，已处理 {0} 个文件。",
                "[Localization] 轉換完成，已處理 {0} 個檔案。", "[Localization] 変換完了。{0} 個のファイルを処理しました。",
                "[Localization] 변환 완료. {0}개 파일 처리."
            },
            ["log.no.changed.source"] = new[]
            {
                "[Localization] No changed source files or SO files detected. Skipped conversion.",
                "[Localization] 未检测到变更的源文件或 SO 文件，跳过转换。", "[Localization] 未偵測到變更的來源檔案或 SO 檔案，略過轉換。",
                "[Localization] 変更されたソースまたは SO ファイルがないため、変換をスキップしました。",
                "[Localization] 변경된 원본 파일 또는 SO 파일이 없어 변환을 건너뜁니다."
            },
            ["log.empty.source.skip"] = new[]
            {
                "[Localization] Source file produced no entries. Skipped SO update: {0}",
                "[Localization] 源文件未产生任何条目，跳过 SO 更新：{0}", "[Localization] 來源檔案沒有產生任何項目，略過 SO 更新：{0}",
                "[Localization] ソースファイルから項目が生成されませんでした。SO 更新をスキップ: {0}",
                "[Localization] 원본 파일에서 항목이 생성되지 않아 SO 업데이트를 건너뜁니다: {0}"
            },
            ["log.created.so"] = new[]
            {
                "[Localization] Created new SO: {0}", "[Localization] 已创建新 SO：{0}", "[Localization] 已建立新 SO：{0}",
                "[Localization] 新しい SO を作成しました: {0}", "[Localization] 새 SO 생성: {0}"
            },
            ["log.updated.so"] = new[]
            {
                "[Localization] Updated existing SO: {0}", "[Localization] 已更新现有 SO：{0}", "[Localization] 已更新現有 SO：{0}",
                "[Localization] 既存 SO を更新しました: {0}", "[Localization] 기존 SO 업데이트: {0}"
            },
            ["log.hash.load.failed"] = new[]
            {
                "[Localization] Failed to load hashes json: {0}", "[Localization] 加载哈希 json 失败：{0}",
                "[Localization] 載入雜湊 json 失敗：{0}", "[Localization] ハッシュ json の読み込みに失敗しました: {0}",
                "[Localization] 해시 json 로드 실패: {0}"
            },
            ["log.stale.source.cleaned"] = new[]
            {
                "[Localization] Source file was deleted; removed its hash record: {0}. Its SO remains for manual review: {1}",
                "[Localization] 源文件已删除，已移除其哈希记录：{0}。对应 SO 保留待人工确认：{1}",
                "[Localization] 來源檔案已刪除，已移除其雜湊記錄：{0}。對應 SO 保留待人工確認：{1}",
                "[Localization] ソースファイルが削除されたためハッシュ記録を削除しました: {0}。対応する SO は手動確認用に残しています: {1}",
                "[Localization] 삭제된 소스 파일의 해시 기록을 제거했습니다: {0}. 해당 SO는 수동 확인용으로 남겨둡니다: {1}"
            },
            ["log.source.file.read.failed"] = new[]
            {
                "[Localization] Could not read file, skipped this conversion: {0}", "[Localization] 无法读取文件，已跳过本次转换：{0}",
                "[Localization] 無法讀取檔案，已略過本次轉換：{0}", "[Localization] ファイルを読み込めないため、この変換をスキップしました: {0}",
                "[Localization] 파일을 읽을 수 없어 이번 변환을 건너뜀: {0}"
            },
            ["log.unsupported.source"] = new[]
            {
                "[LocalizationSourceParser] Unsupported source file format: {0}",
                "[LocalizationSourceParser] 不支持的源文件格式：{0}", "[LocalizationSourceParser] 不支援的來源檔案格式：{0}",
                "[LocalizationSourceParser] 未対応のソース形式: {0}", "[LocalizationSourceParser] 지원하지 않는 원본 형식: {0}"
            },
            ["log.unsupported.export"] = new[]
            {
                "[LocalizationSourceExporter] Unsupported export file format: {0}",
                "[LocalizationSourceExporter] 不支持的导出文件格式：{0}", "[LocalizationSourceExporter] 不支援的匯出檔案格式：{0}",
                "[LocalizationSourceExporter] 未対応のエクスポート形式: {0}", "[LocalizationSourceExporter] 지원하지 않는 내보내기 형식: {0}"
            },
            ["log.csv.importing"] = new[]
            {
                "[CsvParser] Importing CSV - header has {0} columns, keyIndex={1}",
                "[CsvParser] 正在导入 CSV - 表头有 {0} 列，keyIndex={1}", "[CsvParser] 正在匯入 CSV - 表頭有 {0} 欄，keyIndex={1}",
                "[CsvParser] CSV をインポート中 - ヘッダー {0} 列、keyIndex={1}", "[CsvParser] CSV 가져오는 중 - 헤더 {0}열, keyIndex={1}"
            },
            ["log.csv.import.complete"] = new[]
            {
                "[CsvParser] Import complete - {0} entries parsed from CSV", "[CsvParser] 导入完成 - 已从 CSV 解析 {0} 个条目",
                "[CsvParser] 匯入完成 - 已從 CSV 解析 {0} 個項目", "[CsvParser] インポート完了 - CSV から {0} 件を解析しました",
                "[CsvParser] 가져오기 완료 - CSV에서 {0}개 항목 분석"
            },
            ["log.csv.not.found"] = new[]
            {
                "[CsvParser] CSV not found: {0}", "[CsvParser] 找不到 CSV：{0}", "[CsvParser] 找不到 CSV：{0}",
                "[CsvParser] CSV が見つかりません: {0}", "[CsvParser] CSV를 찾을 수 없습니다: {0}"
            },
            ["log.csv.imported"] = new[]
            {
                "[CsvParser] Imported {0} entries from {1}", "[CsvParser] 已从 {1} 导入 {0} 个条目",
                "[CsvParser] 已從 {1} 匯入 {0} 個項目", "[CsvParser] {1} から {0} 件をインポートしました", "[CsvParser] {1}에서 {0}개 항목 가져옴"
            },
            ["log.csv.exported"] = new[]
            {
                "[CsvParser] Exported {0} entries to {1}", "[CsvParser] 已导出 {0} 个条目到 {1}",
                "[CsvParser] 已匯出 {0} 個項目到 {1}", "[CsvParser] {0} 件を {1} にエクスポートしました", "[CsvParser] {0}개 항목을 {1}로 내보냄"
            },
            ["log.xlsx.not.found"] = new[]
            {
                "[XlsxLocalizationParser] XLSX not found: {0}", "[XlsxLocalizationParser] 找不到 XLSX：{0}",
                "[XlsxLocalizationParser] 找不到 XLSX：{0}", "[XlsxLocalizationParser] XLSX が見つかりません: {0}",
                "[XlsxLocalizationParser] XLSX를 찾을 수 없습니다: {0}"
            },
            ["log.xlsx.imported"] = new[]
            {
                "[XlsxLocalizationParser] Imported {0} entries from {1}", "[XlsxLocalizationParser] 已从 {1} 导入 {0} 个条目",
                "[XlsxLocalizationParser] 已從 {1} 匯入 {0} 個項目", "[XlsxLocalizationParser] {1} から {0} 件をインポートしました",
                "[XlsxLocalizationParser] {1}에서 {0}개 항목 가져옴"
            },
            ["log.xlsx.import.failed"] = new[]
            {
                "[XlsxLocalizationParser] Failed to import XLSX: {0}\n{1}",
                "[XlsxLocalizationParser] 导入 XLSX 失败：{0}\n{1}", "[XlsxLocalizationParser] 匯入 XLSX 失敗：{0}\n{1}",
                "[XlsxLocalizationParser] XLSX のインポートに失敗しました: {0}\n{1}",
                "[XlsxLocalizationParser] XLSX 가져오기 실패: {0}\n{1}"
            },
            ["log.xlsx.sheet.importing"] = new[]
            {
                "[XlsxLocalizationParser] Importing sheet '{0}' - header has {1} columns, keyIndex={2}",
                "[XlsxLocalizationParser] 正在导入工作表 '{0}' - 表头有 {1} 列，keyIndex={2}",
                "[XlsxLocalizationParser] 正在匯入工作表 '{0}' - 表頭有 {1} 欄，keyIndex={2}",
                "[XlsxLocalizationParser] シート '{0}' をインポート中 - ヘッダー {1} 列、keyIndex={2}",
                "[XlsxLocalizationParser] 시트 '{0}' 가져오는 중 - 헤더 {1}열, keyIndex={2}"
            },
            ["log.xlsx.sheet.complete"] = new[]
            {
                "[XlsxLocalizationParser] Sheet '{0}' complete - {1} entries parsed",
                "[XlsxLocalizationParser] 工作表 '{0}' 完成 - 已解析 {1} 个条目",
                "[XlsxLocalizationParser] 工作表 '{0}' 完成 - 已解析 {1} 個項目",
                "[XlsxLocalizationParser] シート '{0}' 完了 - {1} 件を解析しました",
                "[XlsxLocalizationParser] 시트 '{0}' 완료 - {1}개 항목 분석"
            },
            ["log.xlsx.sheet.skipped"] = new[]
            {
                "[XlsxLocalizationParser] Skipped sheet without a Key header: {0}",
                "[XlsxLocalizationParser] 已跳过没有 Key 表头的工作表：{0}",
                "[XlsxLocalizationParser] 已跳過沒有 Key 表頭的工作表：{0}",
                "[XlsxLocalizationParser] Key ヘッダーのないシートをスキップしました: {0}",
                "[XlsxLocalizationParser] Key 헤더가 없는 시트를 건너뛰었습니다: {0}"
            },
            ["log.xlsx.exported"] = new[]
            {
                "[XlsxLocalizationExporter] Exported {0} entries to {1}", "[XlsxLocalizationExporter] 已导出 {0} 个条目到 {1}",
                "[XlsxLocalizationExporter] 已匯出 {0} 個項目到 {1}", "[XlsxLocalizationExporter] {0} 件を {1} にエクスポートしました",
                "[XlsxLocalizationExporter] {0}개 항목을 {1}로 내보냄"
            },
            ["log.xlsx.export.failed"] = new[]
            {
                "[XlsxLocalizationExporter] Failed to export XLSX: {0}\n{1}",
                "[XlsxLocalizationExporter] 导出 XLSX 失败：{0}\n{1}", "[XlsxLocalizationExporter] 匯出 XLSX 失敗：{0}\n{1}",
                "[XlsxLocalizationExporter] XLSX のエクスポートに失敗しました: {0}\n{1}",
                "[XlsxLocalizationExporter] XLSX 내보내기 실패: {0}\n{1}"
            },
            ["xlsx.no.key.sheet"] = new[]
            {
                "No worksheet with a Key header was found.", "没有找到包含 Key 表头的工作表。", "找不到包含 Key 表頭的工作表。",
                "Key ヘッダーを含むシートが見つかりません。", "Key 헤더가 있는 시트를 찾을 수 없습니다."
            },
        };

        public static string T(string key)
        {
            if (!Texts.TryGetValue(key, out string[] values))
                return key;

            int languageIndex = LocalizationEditorLanguage.GetTextLanguageIndex();
            if (languageIndex < 0 || languageIndex >= values.Length)
                languageIndex = 0;

            return values[languageIndex];
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }
}
#endif