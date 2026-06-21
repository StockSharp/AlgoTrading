# EMA RSI トレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、長期EMAでフィルタリングされた短期・中期EMAのクロスオーバーを売買します。RSIレベルで決済し、固定ストップロス付きのトレーリングストップでリスクを管理します。利益が出ている場合、指定バー数後にオプションで決済できます。

## 詳細

- **エントリー条件**: EMA AがEMA Bをクロスし、EMA Cとローソク足の方向でトレンドを確認。
- **ロング/ショート**: 両方。
- **エグジット条件**: RSI閾値、トレーリングストップ、または時間ベースのエグジット。
- **ストップ**: 固定パーセンテージのストップで、価格が`TrailOffset`分動いた後にトレーリングストップに転換。
- **デフォルト値**:
  - `EmaALength` = 10
  - `EmaBLength` = 20
  - `EmaCLength` = 100
  - `RsiLength` = 14
  - `ExitLongRsi` = 70
  - `ExitShortRsi` = 30
  - `TrailPoints` = 50
  - `TrailOffset` = 10
  - `FixStopLossPercent` = 5
  - `CloseAfterXBars` = true
  - `XBars` = 24
  - `ShowLong` = true
  - `ShowShort` = false
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, RSI
  - ストップ: トレーリング
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
