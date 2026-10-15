# PSAR トレーダー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

PSAR トレーダー戦略は、Parabolic SAR インジケーターの変化に基づいて行動します。SAR が価格の下に移動するとロングポジションを開き、SAR が価格の上に移動するとショートポジションを開きます。オプションの「Close On Opposite」設定は、反対のシグナルが現れたときにポジションを反転させます。取引は設定されたセッション時間中のみ行われます。ストップロスとテイクプロフィットは保護モジュールによって管理されます。

## 詳細

- **エントリー条件**: 価格が Parabolic SAR を交差すること。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対の SAR 交差またはポジション反転。
- **ストップ**: あり、パラメーターで固定。
- **デフォルト値**:
  - `SarStep` = 0.001m
  - `SarMaxStep` = 0.2m
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `TakeValue` = 50 (absolute)
  - `StopValue` = 50 (absolute)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR
  - ストップ: 固定
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
