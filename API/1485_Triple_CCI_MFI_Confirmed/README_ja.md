# Triple CCI MFI 確認済み戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高速CCIがゼロを上抜け、中期・低速CCIが正値を維持し、価格がEMAを上回り、MFIが50を超えたときにロングエントリーします。ATRベースの活性化後、EMAでトレーリング利益確定を行います。

テストでは中程度のパフォーマンスを示し、トレンド相場で最も効果的です。

## 詳細
- **エントリー条件**:
  - **ロング**: 高速CCIが0を上抜け、中期CCI > 0、低速CCI > 0、MFI > 50、EMAを上回る終値
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - **ロング**: 活性化後にトレーリングEMAを下回る終値、または安値がATRストップに達する
- **ストップ**: あり。
- **デフォルト値**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 14
  - `MiddleCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `MfiLength` = 14
  - `EmaLength` = 50
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: CCI, MFI, EMA, ATR
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
