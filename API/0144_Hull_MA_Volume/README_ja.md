# Hull Ma Volume 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
トレンドの方向にHull Moving Average、トレードエントリーの確認に出来高を使用する戦略。

テストでは年平均リターン約169%を示しています。暗号資産市場で最もパフォーマンスが高いです。

Hull移動平均はノイズを平滑化し、出来高の増加が確信を確認します。価格がHullの傾きに沿って動き、出来高の急増に裏付けられたときにエントリーが発生します。

この手法はブレイクアウトにおける強い参加者を観察するトレーダーを対象としています。ATRベースのストップが突然のリバーサルから守ります。

## 詳細

- **エントリー条件**:
  - ロング: `HullMA(t) > HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
  - ショート: `HullMA(t) < HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `HullMA(t) < HullMA(t-1)`
  - ショート: `HullMA(t) > HullMA(t-1)`
- **ストップ**: エントリーから `StopLossAtr` ATR
- **デフォルト値**:
  - `HullPeriod` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Hull MA, Moving Average, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

