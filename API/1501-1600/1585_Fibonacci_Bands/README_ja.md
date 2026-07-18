# Fibonacciバンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ケルトナーチャネルをFibonacci比率で拡張し、価格が外側のバンドを突破してRSIが確認したときにトレードします。

## 詳細

- **エントリー条件**: 価格が`fbUpper3`を越えてRSIが60以上でロング；`fbLower3`を割ってRSIが40以下でショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: 価格が移動平均線を再び越える。
- **ストップ**: なし。
- **デフォルト値**:
  - `MaType` = WMA
  - `MaLength` = 233
  - `Fib1` = 1.618
  - `Fib2` = 2.618
  - `Fib3` = 4.236
  - `KcMultiplier` = 2
  - `KcLength` = 89
  - `RsiLength` = 14
  - `CandleType` = 5 minutes
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 両方
  - インジケーター: MA, ATR, RSI
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
