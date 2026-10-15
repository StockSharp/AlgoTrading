# マルチバンド比較
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

マルチバンド比較はSMA、標準偏差、価格分位数バンドを使用します。価格が上位分位数から標準偏差を引いた水準を、定義されたバー数以上上回って引けるとロングに入り、その水準を設定されたバー数以上下回るとエグジットします。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: （上位分位数 - 標準偏差）を`EntryConfirmBars`本のバー連続して上回って引ける。
- **エグジット条件**: その水準を`ExitConfirmBars`本のバー連続して下回って引ける。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 20
  - `BollingerMultiplier` = 1
  - `UpperQuantile` = 0.95
  - `EntryConfirmBars` = 1
  - `ExitConfirmBars` = 1
- **フィルター**:
  - カテゴリ: 統計
  - 方向: ロング
  - インジケーター: SMA, Standard Deviation
  - 複雑さ: 中程度
  - リスクレベル: 中
