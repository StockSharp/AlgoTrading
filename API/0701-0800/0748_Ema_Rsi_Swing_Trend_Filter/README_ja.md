# EMA RSIスイング・トレンドフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、EMA200のトレンドフィルターの方向に従ってEMA20とEMA50のクロスオーバーを取引します。
オプションのRSIフィルターにより、RSIが買われすぎの場合はロングエントリーを、売られすぎの場合はショートエントリーを制限します。

## 詳細

- **エントリー条件**: EMA20がEMA50をクロスし、EMA200に対する価格の位置とオプションのRSIフィルターを確認。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆EMAクロスでのオプションのエグジット。
- **ストップ**: なし。
- **デフォルト値**:
  - `EmaFastPeriod` = 20
  - `EmaSlowPeriod` = 50
  - `EmaTrendPeriod` = 200
  - `RsiLength` = 14
  - `UseRsiFilter` = true
  - `RsiMaxLong` = 70
  - `RsiMinShort` = 30
  - `RequireCloseConfirm` = true
  - `ExitOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, RSI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
