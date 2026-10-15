# ユニバーサル・トレーリングストップ・ヘッジ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オープンポジションを保護するためのさまざまなトレーリングストップ技術を示す戦略。
ATR、Parabolic SAR、移動平均、パーセンテージ、固定pipsベースのトレーリングストップを提供します。
足の方向に基づくシンプルなエントリーは純粋に教育目的で使用されています。

## 詳細

- **エントリー条件**：足が始値より高く引けた場合はロング、低く引けた場合はショート
- **ロング/ショート**：両方
- **エグジット条件**：トレーリングストップが発動
- **ストップ**：選択したモードに応じてATR、Parabolic SAR、移動平均、利益パーセンテージまたは固定pips
- **デフォルト値**：
  - `Mode` = `TrailingModes.Atr`
  - `Delta` = 10
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `MaPeriod` = 34
  - `PercentProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**：
  - カテゴリ：リスク管理
  - 方向：両方
  - インジケーター：ATR, Parabolic SAR, SMA
  - ストップ：トレーリングストップ
  - 複雑さ：中級
  - 時間軸：イントラデイ
  - 季節性：いいえ
  - ニューラルネットワーク：いいえ
  - ダイバージェンス：いいえ
  - リスクレベル：中
