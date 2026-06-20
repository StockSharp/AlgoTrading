# 決算発表リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**決算発表リバーサル**戦略は、決算発表日に直近の勝ち組をショートし、直近の負け組を買います。

## 詳細
- **エントリー条件**: 決算日に、最近のリターンがプラスの株をショートし、マイナスの株を買う。
- **ロング/ショート**: 両方。
- **エグジット条件**: シグナル後にポジションを調整；明示的な保有ルールなし。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `LookbackDays = 5`
  - `HoldingDays = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: Event-driven
  - 方向: 両方
  - インジケーター: Returns
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
