# 長期収益スイング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、高速EMAが低速EMAを上回るクロスが発生し、RSIが指定した閾値を上回ったときにロングでエントリーします。価格がATRベースのストップロスまたはテイクプロフィットレベルに達したときにエグジットします。

## 詳細

- **エントリー条件**:
  - ロング: 高速EMAが低速EMAを上回るクロスし、RSI > 閾値。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - 価格がATRベースのストップロスまたはテイクプロフィットに達する。
- **ストップ**: ストップロスとテイクプロフィットのATR倍数。
- **デフォルト値**:
  - `FastEmaLength` = 16
  - `SlowEmaLength` = 30
  - `RsiLength` = 9
  - `AtrLength` = 21
  - `RsiThreshold` = 50
  - `AtrStopMult` = 8
  - `AtrTpMult` = 11
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: EMA, RSI, ATR
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
