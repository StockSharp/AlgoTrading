# Arsi Vwap Atr戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

適応型RSI戦略で、買われすぎと売られすぎのレベルがATRまたはVWAPからの乖離に基づいて拡大または縮小します。RSIが適応レベルをクロスした際にポジションを建て、RSIが中間ゾーンに戻った際に決済します。

## 詳細

- **エントリー条件**:
  - ロング: `RSI` が適応的な売られすぎラインを上抜け
  - ショート: `RSI` が適応的な買われすぎラインを下抜け
- **ロング/ショート**: 両方
- **エグジット条件**:
  - RSIが50または反対の適応ラインを再クロス
- **ストップ**: `StopLossPercent` と `RiskReward` を使用したパーセントベース
- **デフォルト値**:
  - `RsiLength` = 14
  - `BaseK` = 1m
  - `RiskPercent` = 2m
  - `StopLossPercent` = 2.5m
  - `RiskReward` = 2m
  - `SourceOb` = ATR
  - `SourceOs` = ATR
  - `AtrLengthOb` = 14
  - `AtrLengthOs` = 14
  - `ObMultiplier` = 10m
  - `OsMultiplier` = 10m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: RSI, ATR, VWAP
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
