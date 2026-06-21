# RSI 長期戦略 15分足
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、RSIの売られすぎシグナルを長期移動平均と出来高確認と組み合わせてロングポジションに入ります。RSIが30を下回り、SMA(250)がSMA(500)を上回り、出来高が20期間SMAの2.5倍を超えたときに買いを入れます。

## 詳細

- **エントリー条件**: RSIが30未満、SMA(250)がSMA(500)を上回り、出来高が20期間SMAの2.5倍超
- **ロング/ショート**: ロングのみ
- **エグジット条件**: SMA(250)がSMA(500)を下抜けるか、ストップロス発動
- **ストップ**: はい、固定パーセンテージ
- **デフォルト値**:
  - `RsiLength` = 10
  - `VolumeSmaLength` = 20
  - `Sma1Length` = 250
  - `Sma2Length` = 500
  - `VolumeMultiplier` = 2.5
  - `StopLossPercent` = 5
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: RSI, SMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
