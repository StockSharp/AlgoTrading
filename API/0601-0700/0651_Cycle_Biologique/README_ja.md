# Cycle Biologique戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Cycle Biologique戦略は正弦波ベースのサイクルを取引します。サイクルがゼロを上回ったときにロングエントリーし、ゼロを下回ったときにポジションをクローズします。

## 詳細

- **エントリー条件**: サイクルがゼロを上抜けた時。
- **エグジット条件**: サイクルがゼロを下抜けた時。
- **デフォルト値**:
  - `CycleLength` = 30
  - `Amplitude` = 1.0
  - `Offset` = 0
- **フィルター**:
  - カテゴリ: Cycle
  - 方向: ロング
  - インジケーター: Sine Wave
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
