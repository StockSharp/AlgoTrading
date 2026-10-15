# Fractal RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Fractal RSIインジケーターに基づく適応型戦略。
Fractal RSIは価格動向のフラクタル次元を使用してRSI計算の長さを調整し、
オシレーターがトレンド相場ではより早く、レンジ相場ではより遅く反応できるようにします。

戦略はインジケーターが事前定義されたレベルをクロスした時にポジションを開きます。
選択したモードに応じて、検出されたトレンドに沿って、またはそれに逆らって取引できます。

## 詳細

- **エントリー条件**:
  - *トレンドモード - ダイレクト*:
    - 買い: 値が`LowLevel`を下方クロス
    - 売り: 値が`HighLevel`を上方クロス
  - *トレンドモード - アゲインスト*:
    - 買い: 値が`HighLevel`を上方クロス
    - 売り: 値が`LowLevel`を下方クロス
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: オプションの固定ストップロスとテイクプロフィット
- **デフォルト値**:
  - `CandleType` = `TimeSpan.FromHours(4).TimeFrame()`
  - `FractalPeriod` = 30
  - `NormalSpeed` = 30
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `StopLoss` = 1000ポイント
  - `TakeProfit` = 2000ポイント
- **フィルター**:
  - カテゴリ: トレンド / オシレーター
  - 方向: 両方
  - インジケーター: Fractal Dimension, RSI
  - ストップ: はい
  - 複雑さ: 高度なインジケーター使用
  - 時間軸: 4H（設定可能）
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
