# MACDクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

指定されたゾーン内でのMACDクロスオーバーに基づく戦略。

MACDクロスオーバー戦略は、MACDの値が下限と上限の閾値の間に収まっている間に、MACDラインがシグナルラインをクロスするのを待ちます。反対方向のクロスで既存のポジションを決済します。ストップロスは適用されません。

## 詳細

- **エントリー条件**: ゾーン内でのMACDクロスオーバー。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `LowerThreshold` = -0.5m
  - `UpperThreshold` = 0.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
