# Fibonacci スイングトレーディングボット
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Fibonacciリトレースメントレベルを使用してスイング動向を取引する戦略。

このボットは直近50バーのレンジから0.618と0.786のリトレースメントレベルを計算し、ローソク足がこれらのレベルを上下にブレイクアウトしたときにポジションを開きます。リスク管理は設定可能なストップロスとリスク/リワード比パラメータで行います。

## 詳細

- **エントリー条件**: FibonacciレベルによるPrice Action。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: はい、パーセントベース。
- **デフォルト値**:
  - `FiboLevel1` = 0.618
  - `FiboLevel2` = 0.786
  - `RiskRewardRatio` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: スイング
  - 方向: 両方
  - インジケーター: Fibonacci, Donchian
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 4h
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

