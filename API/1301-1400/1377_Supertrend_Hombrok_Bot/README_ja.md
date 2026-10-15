# Supertrend Hombrok Bot戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

出来高、ローソク足の実体サイズ、RSIフィルターを使用し、ATRベースのストップとテイクプロフィットを備えたSupertrend戦略。

## 詳細
- **エントリー条件**: 出来高・実体フィルターを満たしRSIが買われすぎを下回る上昇トレンドでロング；フィルターを満たしRSIが売られすぎを上回る下降トレンドでショート
- **ロング/ショート**: 両方
- **エグジット条件**: ATRベースのストップロスまたはテイクプロフィット
- **ストップ**: ATRからの固定ストップとテイクプロフィット
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70m
  - `RsiOversold` = 30m
  - `VolumeMultiplier` = 1.2m
  - `BodyPctOfAtr` = 0.3m
  - `RiskRewardRatio` = 2m
  - `CapitalPerTrade` = 10m
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Supertrend, RSI, ATR, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
