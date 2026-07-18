# 自社株買いを伴う決算発表戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**自社株買いを伴う決算発表**戦略は、アクティブな自社株買いプログラムを実施している株式を、決算発表の数日前に買い、報告後まもなく手仕舞います。

## 詳細
- **エントリー条件**: 会社がアクティブな自社株買いを行っている場合、決算の `DaysBefore` 日前に買う。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 決算日の `DaysAfter` 日後に売る。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: Event-driven
  - 方向: ロング
  - インジケーター: Buyback + Calendar
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
