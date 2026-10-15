# 利益品質ファクター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Earnings Quality Factor** 戦略は、毎年7月1日にリバランスを行い、利益品質スコアに基づいて高品質株をロング、低品質株をショートします。

## 詳細
- **エントリー条件**: 品質スコアを用いた毎年7月1日の年次リバランス。
- **ロング/ショート**: 両方。
- **エグジット条件**: 次回の年次リバランス。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `MinTradeUsd = 100`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: ファンダメンタル
  - 方向: 両方
  - インジケーター: 品質
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
