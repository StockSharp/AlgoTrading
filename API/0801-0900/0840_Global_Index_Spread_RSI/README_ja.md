# グローバルインデックス スプレッドRSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Global Index Spread RSIは、E-mini S&P 500とグローバル株式インデックスとのスプレッドが売られ過ぎになった際に取引します。スプレッドはパーセント単位で計算され、短期RSIに通されます。RSIが売られ過ぎ閾値を下回るとロングポジションを建て、買われ過ぎ閾値を上回ると決済します。

## 詳細
- **データ**: ESとグローバルインデックスの日次終値。
- **エントリー条件**:
  - **ロング**: スプレッドRSIが`OversoldThreshold`を下回る。
- **エグジット条件**: スプレッドRSIが`OverboughtThreshold`を上回る。
- **ストップ**: なし。
- **デフォルト値**:
  - `RsiLength` = 2
  - `OversoldThreshold` = 35
  - `OverboughtThreshold` = 78
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: RSI
  - 複雑さ: 低
  - リスクレベル: 中
