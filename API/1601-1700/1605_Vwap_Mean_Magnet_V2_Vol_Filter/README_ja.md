# VWAP Mean Magnet v2（出来高フィルター）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はVWAP平均回帰のコンセプトとRSIおよび出来高フィルターを組み合わせます。価格がVWAPから乖離しRSIが極端な水準に達し、かつ現在の出来高が移動平均に倍率を掛けた値を上回ったときにトレードを行います。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格 < VWAP、RSI < 売られすぎ、出来高フィルター通過。
  - **ショート**: 価格 > VWAP、RSI > 買われすぎ、出来高フィルター通過。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 価格がVWAPに戻ったときにポジションを決済。
- **ストップ**: あり、パーセンテージ・ストップロス。
- **デフォルト値**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Volume lookback` = 20
  - `Volume multiplier` = 3
  - `Stop loss %` = 0.5
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: あり
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
