# Z-Score RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Z-Score RSI戦略は価格のZ-ScoreのRSIを計算し、RSIのEMAをシグナルに使用する。RSIがEMAを上回るクロスでロング、下回るクロスでショートポジションを建てる。

## 詳細

- **エントリー条件**: Z-ScoreのRSIがそのEMAをクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のクロス
- **ストップ**: なし
- **デフォルト値**:
  - `ZScoreLength` = 20
  - `RsiLength` = 9
  - `SmoothingLength` = 15
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: SMA, StandardDeviation, RSI, EMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
