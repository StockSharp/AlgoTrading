# Supertrend戦略 (5m)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

5分足ローソク足を使ったSupertrend戦略。

## 詳細

- **エントリー条件**: 価格がSupertrendを上抜け。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 価格がSupertrendを下抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: ATR, Supertrend
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
