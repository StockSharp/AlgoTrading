# Warrior Trading モメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ギャップ検出、VWAP、レッド・ツー・グリーン・セットアップを組み合わせたWarrior Trading流のモメンタム戦略。

## 詳細

- **エントリー条件**: Gap-and-go、red-to-green、または出来高急増を伴うVWAPバウンス。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: ATRベースのストップ、利益目標、トレーリング。
- **ストップ**: はい。
- **デフォルト値**:
  - `GapThreshold` = 2m
  - `GapVolumeMultiplier` = 2m
  - `VwapDistance` = 0.5m
  - `MinRedCandles` = 3
  - `RiskRewardRatio` = 2m
  - `TrailingStopTrigger` = 1m
  - `MaxDailyTrades` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロング
  - インジケーター: VWAP, RSI, EMA, ATR, Volume
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
