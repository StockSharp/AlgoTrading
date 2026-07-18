# EMA MACD RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAによるトレンドフィルター、MACDクロスオーバー、RSIレベルを組み合わせた戦略。

高速EMAが低速EMAを上回り、MACDがシグナルラインを上抜け、RSIがRsiBuyLevelと70の間にあるときに買います。高速EMAが低速EMAを下回り、MACDがシグナルラインを下抜け、RSIが30とRsiSellLevelの間にあるときに売ります。

## 詳細

- **エントリー条件**: EMAによるトレンドフィルター、MACDクロスオーバー、RSIレベル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuyLevel` = 45m
  - `RsiSellLevel` = 55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, MACD, RSI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
