# マルチインジケーター・トレンドフォロー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIとボリューム確認付きのEMAクロスオーバー戦略。ATRベースのストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**: 速いEMAが遅いEMAを上抜け/下抜けし、RSIフィルターと高出来高が確認される
- **ロング/ショート**: 両方
- **エグジット条件**: ATRベースのストップロスとテイクプロフィット
- **ストップ**: はい、ATRベース
- **デフォルト値**:
  - `CandleType` = 5 minute
  - `FastMaLength` = 10
  - `SlowMaLength` = 30
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `AtrPeriod` = 14
  - `StopLossAtrMultiplier` = 2
  - `TakeProfitAtrMultiplier` = 3
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, RSI, ATR, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
