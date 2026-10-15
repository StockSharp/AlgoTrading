# ASCTrendND戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はASCTrendND MQL5エキスパートアドバイザーにインスパイアされています。主要トレンドシグナルとして単純移動平均を使用し、強度を確認するRSIフィルターと取引のエグジットにATRベースのトレーリングストップを使用します。このアプローチはStockSharp高レベルAPIで簡略化された形でASCTrend + NRTR + TrendStrengthロジックを再現しようとしています。

## 詳細

- **エントリー条件:**
  - **ロング:** 終値がSMAより上でRSI > 50。
  - **ショート:** 終値がSMAより下でRSI < 50。
- **エグジット条件:**
  - ATR * 乗数に基づくトレーリングストップまたは反対のシグナル。
- **ストップ:** ATRベースのトレーリングストップのみ。
- **デフォルト値:**
  - `SmaPeriod` = 50
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5分ローソク足
- **フィルター:**
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: SMA, RSI, ATR
  - ストップ: トレーリング
  - 複雑さ: 低
  - 時間軸: 5m
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
