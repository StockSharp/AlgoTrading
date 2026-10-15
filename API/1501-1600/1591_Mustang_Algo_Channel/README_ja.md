# Mustang Algoチャネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

WMAで平滑化したRSIベースのグローバルセンチメントオシレーターを使用して、チャネルクロスオーバーを取引する戦略。

## 詳細

- **エントリー条件**: RSI/WMAオシレーターと境界線のクロスオーバー。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: 反対シグナルまたはストップ/テイク。
- **ストップ**: パーセントベース、オプション。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `Smoothing` = 20
  - `MedianPeriod` = 25
  - `UpperBound` = 55
  - `LowerBound` = 48
  - `TradeMode` = Long & Short
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `StopLossPercent` = 4
  - `TakeProfitPercent` = 12
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 設定可能
  - インジケーター: RSI, WMA
  - ストップ: パーセント
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
