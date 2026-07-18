# ゴールド・スキャルピング戦略（精密エントリー）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAトレンドフィルター、RSIレンジ、エングルフィングパターンを使用したゴールド向けスキャルピング戦略。

## 詳細

- **エントリー条件**: EMAトレンドフィルターでRSIが45から55の間、かつEMA50付近で強気/弱気のエングルフィングパターン。
- **ロング/ショート**: 両方向。
- **エグジット条件**: テイクプロフィットまたはストップロス。
- **ストップ**: ATRベースのストップロスと固定Pipターゲット。
- **デフォルト値**:
  - `EmaFastPeriod` = 50
  - `EmaSlowPeriod` = 200
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `RsiLower` = 45
  - `RsiUpper` = 55
  - `PipTarget` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: EMA, RSI, ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
