# マルチ・コンフルエンス・スイングハンター V1 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

マルチ・コンフルエンス・スイングハンター V1 戦略は、RSI、MACD、価格アクションを組み合わせたスコアリングシステムを使用してスイングの安値と高値を識別します。強気シグナルが最低エントリースコアに達するとロングトレードが開かれ、弱気シグナルがエグジットスコアに達すると決済されます。

## 詳細

- **エントリー条件**: RSI/MACDシグナルと強気ローソク足構造からのエントリースコア ≥ `MinEntryScore`。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: RSI/MACDシグナルと弱気ローソク足構造からのエグジットスコア ≥ `MinExitScore`。
- **ストップ**: なし。
- **デフォルト値**:
  - `MacdFast` = 3
  - `MacdSlow` = 10
  - `MacdSignal` = 3
  - `RsiLength` = 21
  - `MinEntryScore` = 13
  - `MinExitScore` = 13
  - `MinLowerWickPercent` = 50
  - `RsiOversold` = 30
  - `RsiExtremeOversold` = 25
  - `RsiOverbought` = 70
  - `RsiExtremeOverbought` = 75
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: ロングのみ
  - インジケーター: RSI, MACD
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
