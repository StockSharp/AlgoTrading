# タイム・トレーダー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

指定した時刻にちょうどロングおよび/またはショートポジションを建て、設定可能なテイクプロフィットとストップロスでポジションを保護する時間ベースの戦略です。

## 詳細

- **エントリー条件**: `TradeHour:TradeMinute:TradeSecond` に `AllowBuy` ならロングを、`AllowSell` ならショートを開く。
- **ロング/ショート**: 両方（設定に依存）
- **エグジット条件**: ストップロスまたはテイクプロフィットによりポジションをクローズ
- **ストップ**: はい、両方
- **デフォルト値**:
  - `Volume` = 1
  - `TakeProfit` = 0.2
  - `StopLoss` = 0.2
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `TradeSecond` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `CandleType` = TimeSpan.FromSeconds(1).TimeFrame()
- **フィルター**:
  - カテゴリ: 時間
  - 方向: 両方
  - インジケーター: なし
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

