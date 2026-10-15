# Supertrend と MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supertrend、MACD、EMA 200フィルターを組み合わせた戦略。

## 詳細

- **エントリー条件**: SupetrendとEMAに対する価格、MACDラインとシグナルラインの比較。
- **ロング/ショート**: 両方。
- **エグジット条件**: MACDクロスオーバーまたは直近の極値に基づくストップ。
- **ストップ**: 最高値/最安値のトレーリングストップ。
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `Factor` = 3
  - `EmaPeriod` = 200
  - `StopLookback` = 10
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SuperTrend, EMA, MACD, Highest, Lowest
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
