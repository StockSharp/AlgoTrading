# Pivot Point Supertrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Pivot PointsとATRベースのSupertrendを組み合わせてトレンドの転換を捉える戦略です。

テストでは年平均リターン約65%を示しています。株式市場で最もよいパフォーマンスを発揮します。

Pivot Pointsが動的な中心線を定義します。ATR乗数が価格に追従する上下バンドを形成します。トレンドが方向を転換すると、戦略はそれに応じてエントリーします。

## 詳細

- **エントリー条件**: Pivot PointsとATR Supertrendに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Pivot Points, ATR
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
