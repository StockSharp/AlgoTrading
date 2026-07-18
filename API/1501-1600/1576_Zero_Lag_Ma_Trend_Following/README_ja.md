# Zero-Lag MAトレンドフォロー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ゼロラグMAがEMAをクロスするのを待ち、価格がATRサイズのボックスをブレイクアウトしたときにエントリーするトレンドフォローシステム。ポジションにはリスクリワードベースの目標が含まれる。

## 詳細

- **エントリー条件**: ゼロラグMAクロスとボックスブレイクアウト。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRベースのストップまたはテイクプロフィット。
- **ストップ**: あり。
- **デフォルト値**:
  - `Length` = 34
  - `AtrPeriod` = 14
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ZLEMA, EMA, ATR
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
