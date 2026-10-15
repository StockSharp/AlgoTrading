# Supertrend アドバンス プルバック戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supertrend Advance PullbackはSupertrendをプルバックまたはトレンド転換エントリーと組み合わせます。オプションのEMA、RSI、MACD、CCIフィルターがシグナルを絞り込みます。

## 詳細

- **エントリー条件**: Supetrendのプルバックまたは転換（オプションのEMA、RSI、MACD、CCIフィルターあり）
- **ロング/ショート**: 両方
- **エグジット条件**: 反対シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `AtrLength` = 10
  - `Factor` = 3
  - `EmaLength` = 200
  - `UseEmaFilter` = true
  - `UseRsiFilter` = true
  - `RsiLength` = 14
  - `RsiBuyLevel` = 50
  - `RsiSellLevel` = 50
  - `UseMacdFilter` = true
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseCciFilter` = true
  - `CciLength` = 20
  - `CciBuyLevel` = 200
  - `CciSellLevel` = -200
  - `Mode` = Pullback
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Supertrend, EMA, RSI, MACD, CCI
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
