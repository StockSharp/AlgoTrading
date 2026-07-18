# EMAスコアリング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は3本のEMAラインを使って市場の方向性を評価し、スコアの閾値を超えた際に取引を行います。

## 詳細
- **エントリー条件**:
  - **ロング**: スコアが閾値を上抜け。
  - **ショート**: スコアがマイナスの閾値を下抜け。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `Short EMA Period` = 21
  - `Medium EMA Period` = 50
  - `Long EMA Period` = 100
  - `Score Threshold` = 4
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 中期
