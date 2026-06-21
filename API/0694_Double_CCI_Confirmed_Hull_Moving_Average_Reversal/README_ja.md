# ダブル CCI 確認 Hull MA リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がHull Moving Averageを上抜けし、かつ高速・低速のCCIインジケーターで確認された時にロングエントリーします。ATRベースの起動後、トレーリングEMAで利益を管理します。

テストでは中程度の年間リターンを示します。混合市場で最もよいパフォーマンスを発揮します。

## 詳細
- **エントリー条件**:
  - **ロング**: 価格がHMAを上抜け、終値がHMAより上、高速CCI > 0、低速CCI > 0
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - **ロング**: 起動後にトレーリングEMAを下回るか、安値がATRストップに達する
- **ストップ**: あり。
- **デフォルト値**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `HullMaLength` = 34
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: ロングのみ
  - インジケーター: CCI, HMA, EMA, ATR
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
