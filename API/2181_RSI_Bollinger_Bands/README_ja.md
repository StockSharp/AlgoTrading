# RSI Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI（相対力指数）とBollinger Bandsを組み合わせた戦略。RSIが売られすぎ閾値を下回り、終値が下部Bollinger Bandを下回るとロングポジションを建てます。RSIが買われすぎ閾値を上回り、終値が上部Bollinger Bandを上回るとショートポジションを建てます。逆シグナルが現れるとポジションを反転させます。

## 詳細

- **エントリー条件**: 買いはRSIが`RsiOversold`を下回り終値が下部バンドを下回る；売りはRSIが`RsiOverbought`を上回り終値が上部バンドを上回る。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `RsiPeriod` = 20
  - `BollingerPeriod` = 20
  - `BollingerWidth` = 2
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RSI, Bollinger Bands
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 15分
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
