# Max Pain戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、出来高と価格変動の両方が設定可能な閾値を超え、かつVIX指数が指定レベルを下回っているときにロングポジションを建てます。エントリー時にボラティリティベースのストップロスが設定され、固定期間後にポジションがクローズされます。

## 詳細

- **エントリー条件**:
  - **ロング**: 出来高が平均出来高 × `VolumeMultiplier` を超え、かつ価格変化が前回終値 × `PriceChangeMultiplier` を超え、VIXが `VixThreshold` を下回っている。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - エントリー価格から `StopLossMultiplier` × ボラティリティ下のストップロス。
  - `HoldPeriods` バー後にポジションをクローズ。
- **ストップ**: はい。
- **デフォルト値**:
  - `LookbackPeriod` = 70.
  - `VolumeMultiplier` = 1.
  - `PriceChangeMultiplier` = 0.029.
  - `StopLossMultiplier` = 2.4.
  - `VixThreshold` = 44.
  - `HoldPeriods` = 8.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
  - `VixCandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロングのみ
  - インジケーター: 出来高、価格アクション、ボラティリティ
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
