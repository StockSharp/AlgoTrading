# QQQ向けSupertrend ロングのみ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supertrendインジケーターと日付範囲フィルターに基づくロングのみ戦略。

## 詳細

- **エントリー条件**: 価格がSupertrendを上抜け。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 価格がSupertrendを下抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `AtrPeriod` = 32
  - `Multiplier` = 4.35m
  - `StartDate` = 1995-01-01
  - `EndDate` = 2050-01-01
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: ATR, Supertrend
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
