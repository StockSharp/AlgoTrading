# Connors VIX リバーサル III
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VIX の移動平均に対するスパイクを利用した逆張り戦略。VIX が設定されたパーセンテージで平均を上回ったときに買い、VIX が平均を下回ったときにショートを建てます。

VIX が前日の移動平均をクロスしたときにポジションをクローズします。

## 詳細

- **エントリー条件**: 買いは VIX 安値が MA を上回り終値が MA をしきい値分上回る場合; 売りは VIX 高値が MA を下回り終値がしきい値を下回る場合。
- **ロング/ショート**: 両方。
- **エグジット条件**: VIX が昨日の MA をクロス。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `LengthMA` = 10
  - `PercentThreshold` = 10m
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: 逆張り
  - 方向: 両方
  - インジケーター: VIX, SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
