# ダブル MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ダブル MACDは異なる速度の2つのMACDインジケーターを使用します。両方のMACDが方向一致した時のみポジションを開設します。

最初のMACDは高速で素早く反応します。2番目は低速で、取引前にトレンドを確認します。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: 両方のMACDラインがシグナルラインを上回る。
  - **ショート**: 両方のMACDラインがシグナルラインを下回る。
- **エグジット条件**: 反対のシグナルまたはストップ。
- **ストップ**: オプションのストップロス。
- **デフォルト値**:
  - `FastLength1` = 12
  - `SlowLength1` = 26
  - `SignalLength1` = 9
  - `MaType1` = Ema
  - `FastLength2` = 24
  - `SlowLength2` = 52
  - `SignalLength2` = 9
  - `MaType2` = Ema
  - `StopLossPercent` = 2
  - `CandleType` = tf(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングとショート
  - インジケーター: MACD
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
