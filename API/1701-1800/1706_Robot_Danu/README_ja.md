# Robot Danu戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ローソク足の高値と安値から導出された高速・低速ZigZagレベルを比較する戦略です。
高速ZigZagレベルが低速レベルを上回るとショートポジションを開きます。
高速ZigZagレベルが低速レベルを下回るとロングポジションを開きます。

## 詳細
- **エントリー条件**: 高速・低速ZigZagピボットの比較。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のZigZag関係。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastLength` = 28
  - `SlowLength` = 56
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
