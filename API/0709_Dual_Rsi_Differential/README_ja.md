# デュアル RSI ディファレンシャル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

デュアル RSI ディファレンシャルは 2 つの RSI 期間を比較し、その差が閾値を越えたときに取引します。この二重期間アプローチは、短期および長期モメンタムの乖離を捉えることを目的としています。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: `RSI(Long) - RSI(Short)` < `RsiDiffLevel`。
  - **ショート**: `RSI(Long) - RSI(Short)` > `RsiDiffLevel`。
- **エグジット条件**: 逆方向の閾値、オプションの保有期間、オプションのテイクプロフィット/ストップロス。
- **ストップ**: オプションのテイクプロフィットとストップロス（`Condition`）。
- **デフォルト値**:
  - `ShortRsiPeriod` = 21
  - `LongRsiPeriod` = 42
  - `RsiDiffLevel` = 5
  - `UseHoldDays` = True
  - `HoldDays` = 5
  - `Condition` = None
  - `TakeProfitPerc` = 15
  - `StopLossPerc` = 10
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロングとショート
  - インジケーター: RSI
  - 複雑さ: 基本
  - リスクレベル: 中
