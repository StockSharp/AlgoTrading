# ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平均回帰取引のためのZ-Scoreインジケーターに基づく戦略

テストでは年平均リターン約121%が示されています。暗号通貨市場で最もパフォーマンスが高くなります。

ZScoreは移動平均からの価格偏差を測定します。極端に高いまたは低いZ-scoreは過度な伸長を示し、逆方向のトレードを促します。トレードはZ-scoreが正常化したときに終了します。

Z-Scoreは任意の時系列にスケールできるため、柔軟なフィルターです。ボラティリティ調整された出口を使用することで、変化する市場環境への適応を助けます。


## 詳細

- **エントリー条件**: MA、ZScoreに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `ZScoreEntryThreshold` = 2.0m
  - `ZScoreExitThreshold` = 0.0m
  - `MAPeriod` = 20
  - `StdDevPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: MA, ZScore
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

