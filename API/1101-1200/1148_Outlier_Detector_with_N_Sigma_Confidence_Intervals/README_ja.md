# Nシグマ信頼区間を使った外れ値検出戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Nシグマ信頼区間を使用して価格変化の外れ値を識別し、極端な動きが発生したときに平均回帰を取引します。

## 詳細

- **エントリー条件**:
  - z-score > `SecondLimit` のときショート。
  - z-score < -`SecondLimit` のときロング。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - |z-score| < `FirstLimit` のときポジションをクローズ。
- **ストップ**: なし。
- **デフォルト値**:
  - `SampleSize` = 30
  - `FirstLimit` = 2
  - `SecondLimit` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: StandardDeviation, Z-Score
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - リスクレベル: 中
