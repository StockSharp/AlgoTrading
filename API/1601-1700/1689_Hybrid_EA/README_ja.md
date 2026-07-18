# Hybrid EA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hybrid EA戦略は、相対活力指数（RVI）とそのシグナルラインを使用します。
RVIがシグナルを指定した差分だけ上回るとロングポジションを建て、同じ量だけ下回るとショートポジションを建てます。ポジションは価格ポイントで測定された固定のテイクプロフィットとストップロスレベルで保護されます。

## 詳細

- **エントリー条件**: RVIからシグナルを引いた値が閾値を超える
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向の閾値クロスまたはテイクプロフィット/ストップロス
- **ストップ**: はい、ポイント単位の固定距離
- **デフォルト値**:
  - `Volume` = 1
  - `RviLength` = 10
  - `SignalLength` = 4
  - `DifferenceThreshold` = 0.05
  - `TakeProfit` = 18
  - `StopLoss` = 9
  - `CandleType` = 5 minute candles
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RVI, SMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
