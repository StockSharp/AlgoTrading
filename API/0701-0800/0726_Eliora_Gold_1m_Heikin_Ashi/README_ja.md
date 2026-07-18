# Eliora Gold 1m Heikin Ashi戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は1分足でHeikin Ashiローソク足を使用します。市場がコンソリデーションしていないときにトレンド方向の強いローソク足でエントリーし、取引間にクールダウンを設けます。エグジットはATRベースのトレーリングストップで管理します。

## 詳細

- **エントリー条件**: トレンド方向の強いHeikin Ashiローソク足、コンソリデーションなし、ボラティリティフィルター。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRトレーリングストップ。
- **ストップ**: あり。
- **デフォルト値**:
  - `AtrPeriod` = 14
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Heikin Ashi, ATR, SMA, Highest/Lowest
  - ストップ: あり
  - 複雑さ: 中程度
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
