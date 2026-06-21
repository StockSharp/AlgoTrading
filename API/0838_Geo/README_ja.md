# Geo戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ローソク足の高値/安値の比率が黄金比に近い場合に買いを入れる戦略。

## 詳細

- **エントリー条件**: 高値/安値の比率がphiの許容範囲内。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆の条件。
- **ストップ**: なし。
- **デフォルト値**:
  - `Tolerance` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: Candle ratio
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
