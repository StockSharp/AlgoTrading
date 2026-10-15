# Fearzoneパネル
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

「Framgångsrik Aktiehandel」のFearZoneパネルに着想を得た戦略です。恐怖が支配するパニック売りを探します。

戦略は両方のFearzone指標がアクティブであり、少なくとも1つのパニックトリガーが発動し、かつ価格が200期間移動平均を上回っている状態を待ちます。

## 詳細

- **エントリー条件**: FZ1とFZ2がアクティブ、かつ負のインパルス・跳ね返りゾーン・ストキャスティクス売られすぎのいずれかが発動し、終値がMA200を上回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 価格がMA200を下抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `LookbackPeriod` = 22
  - `BollingerPeriod` = 200
  - `ImpulsePeriod` = 10
  - `ImpulsePercent` = 0.1m
  - `MaPeriod` = 200
  - `StochThreshold` = 30m
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロングのみ
  - インジケーター: BollingerBands, RateOfChange, StochasticOscillator, SimpleMovingAverage, Highest
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
