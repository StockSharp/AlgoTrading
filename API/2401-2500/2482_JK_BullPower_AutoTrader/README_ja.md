# JK BullP 自動売買戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

JK BullP 自動売買戦略は Bulls Power オシレーターに依存する元の MetaTrader Expert Advisor を移植したものです。連続する 2 つの Bulls Power 値の関係を解釈して、強気の強さがゼロライン上方で弱まっているとき、またはゼロを下回ってリバーサルするときを検出します。ロングとショートのトレードは固定ストップと、トレードが利益になるにつれて締まっていく増分トレーリングストップで保護されます。

## 詳細

- **エントリー条件**: 2 バー前の Bulls Power が前バーを上回り、前バーがゼロ以上のとき売り。前バーの Bulls Power がゼロ未満のとき買い。
- **ロング/ショート**: 両方。
- **エグジット条件**: 固定テイクプロフィット、固定ストップロス、またはトレーリングストップに達した場合。反対のシグナルでポジションを反転。
- **ストップ**: 固定テイクプロフィット、固定ストップロス、トレーリングストップ。
- **デフォルト値**:
  - `BullsPeriod` = 13
  - `TakeProfitPoints` = 350
  - `StopLossPoints` = 100
  - `TrailingStopPoints` = 100
  - `TrailingStepPoints` = 40
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Bulls Power
  - ストップ: 固定 + トレーリング
  - 複雑さ: 基本
  - 時間軸: イントラデイ / スイング (1H)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
