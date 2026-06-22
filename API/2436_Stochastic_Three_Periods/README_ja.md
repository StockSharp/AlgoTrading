# Stochastic 三期間戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Stochastic 三期間**戦略は、速いストキャスティクスのシグナルを上位2つの時間軸からの確認と組み合わせます。上位時間軸が両方一致している状態で速いオシレーターがクロスした際にトレードを開始します。

## 詳細

- **エントリー条件**: 速い%Kが%Dをクロスし、`ShiftEntrance`バー前に逆の状態があった場合；上位時間軸の両ストキャスティクスで%Kが%Dを上回っている；終値がシグナルの方向に動いていること。
- **ロング/ショート**: 両方。
- **エグジット条件**: 前のローソク足で計測した速いストキャスティクスの逆クロス。
- **ストップ**: `StartProtection`によるポイント単位の固定ストップロスとテイクプロフィット。
- **デフォルト値**:
  - `CandleType1` = 5m
  - `CandleType2` = 15m
  - `CandleType3` = 30m
  - `KPeriod1` = 5
  - `KPeriod2` = 5
  - `KPeriod3` = 5
  - `KExitPeriod` = 5
  - `ShiftEntrance` = 3
  - `TakeProfitPoints` = 30
  - `StopLossPoints` = 10
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Stochastic
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
