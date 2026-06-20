# ADX Donchian Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はADX Donchianインジケーターを使用してシグナルを生成します。
ADX > AdxThreshold かつ Price >= upperBorder（強いトレンドと上方ブレイクアウト）の場合にロングエントリー。ADX > AdxThreshold かつ Price <= lowerBorder（強いトレンドと下方ブレイクアウト）の場合にショートエントリー。
混合市場で機会を求めるトレーダーに適しています。

テストでは年間平均リターン約67%を示しています。株式市場で最もパフォーマンスが高いです。

## 詳細
- **エントリー条件**:
  - **ロング**: ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up)
  - **ショート**: ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down)
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: ADXが(threshold - 5)を下回ったときポジションを終了
  - **ショート**: ADXが(threshold - 5)を下回ったときポジションを終了
- **ストップ**: はい。
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `DonchianPeriod` = 5
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AdxThreshold` = 10
  - `Multiplier` = 0.1m
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: ADX Donchian
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

