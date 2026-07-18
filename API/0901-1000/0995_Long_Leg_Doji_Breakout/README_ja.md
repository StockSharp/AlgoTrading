# ロングレッグDoji ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ロングレッグDoji ブレイクアウト戦略は、長い脚を持つDojiローソク足を識別し、Doji レンジの上下のブレイクアウトを取引します。オプションのATRフィルターにより、ヒゲが十分に長いことを確認します。

## 詳細

- **エントリー条件**:
  - **ロング**: ブレイクアウト待機中 && close > Dojiの高値 && 前の close <= Dojiの高値。
  - **ショート**: ブレイクアウト待機中 && close < Dojiの安値 && 前の close >= Dojiの安値。
- **ロング/ショート**: 両方。
- **エグジット条件**: 終値がポジションと反対方向にSMA(20)をクロスする。
- **ストップ**: なし。
- **デフォルト値**:
  - `Doji body threshold %` = 0.1
  - `Minimum wick ratio` = 2
  - `Use ATR filter` = true
  - `ATR period` = 14
  - `ATR multiplier` = 0.5
- **フィルター**:
  - カテゴリ: パターンブレイクアウト
  - 方向: 両方
  - インジケーター: ATR, SMA
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
