# SMA RSI 出来高 ATR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は単純移動平均（SMA）、相対力指数（RSI）、出来高確認、ATRベースのボラティリティフィルターを組み合わせます。
価格がSMAを上回り、RSIが売られすぎ、出来高が移動平均の倍数を超え、ボラティリティが上昇しているときに買います。逆の条件で売ります。

ストップは固定パーセンテージのテイクプロフィットとストップロスレベルで管理されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `Close > SMA` && `RSI < RsiOversold` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
  - **ショート**: `Close < SMA` && `RSI > RsiOverbought` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: はい、パーセントベース
- **デフォルト値**:
  - `SmaLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `VolumeThreshold` = 1.5
  - `AtrLength` = 14
  - `TakeProfitPerc` = 1.5
  - `StopLossPerc` = 0.5
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, RSI, 出来高, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
