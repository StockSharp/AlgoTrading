# VWAP Mean Magnet v9（シンプルアラート）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VWAP Mean Magnet戦略の簡略版で、出来高フィルターなしにVWAPとRSIのみを使用します。価格がVWAPから乖離しRSIが極端な水準に達したときにトレードを開始し、価格がVWAPに戻ったときにポジションを決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格 < VWAP かつ RSI < 売られすぎ。
  - **ショート**: 価格 > VWAP かつ RSI > 買われすぎ。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 価格がVWAPに戻ったときにポジションを決済。
- **ストップ**: あり、パーセンテージ・ストップロス。
- **デフォルト値**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Stop loss %` = 0.5
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: あり
  - 複雑さ: シンプル
  - 時間軸: イントラデイ
