# 決算発表プレミアム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**決算発表プレミアム**戦略は、決算発表の数日前に株式を買い、発表直後に手仕舞います。

## 詳細
- **エントリー条件**: 決算の `DaysBefore` 日前に買う。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 決算の `DaysAfter` 日後に売る。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: Event-driven
  - 方向: ロング
  - インジケーター: Calendar
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
