# クリーンなスクリーナーライブラリ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数の銘柄にわたってRSIを評価し、買いまたは売りの評価を出力するシンプルなスクリーナー戦略です。カスタムのマルチアセットスクリーナーを構築するための基盤として機能します。

## 詳細

- **エントリー条件**: 各銘柄のRSI値を閾値と比較する。
- **ロング/ショート**: なし（シグナルのみ）
- **エグジット条件**: なし
- **ストップ**: なし
- **デフォルト値**:
  - `RsiLength` = 14
  - `StrongThreshold` = 70m
  - `WeakThreshold` = 60m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: スクリーナー
  - 方向: N/A
  - インジケーター: RSI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: N/A
