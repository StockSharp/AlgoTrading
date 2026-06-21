# Manadi 売買 EMA MACD RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACD と RSI の確認を伴う EMA クロスオーバー戦略。固定パーセントのストップロスとテイクプロフィットで市場にエントリーします。

## 詳細

- **エントリー条件**: MACD が一致し RSI が範囲内にある状態での EMA クロスオーバー。
- **ロング/ショート**: 両方向。
- **エグジット条件**: パーセントベースのストップロスまたはテイクプロフィット。
- **ストップ**: パーセントベース。
- **デフォルト値**:
  - `FastEmaLength` = 9
  - `SlowEmaLength` = 21
  - `RsiLength` = 14
  - `RsiUpperLong` = 70
  - `RsiLowerLong` = 40
  - `RsiUpperShort` = 60
  - `RsiLowerShort` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `TakeProfitPercent` = 0.03
  - `StopLossPercent` = 0.015
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA、MACD、RSI
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
