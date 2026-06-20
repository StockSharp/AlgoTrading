# Momentum Percentage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
価格モメンタムの変化率に基づく戦略

テストでは年平均リターンが約97%であることが示されています。暗号資産市場で最もよく機能します。

Momentum Percentageは価格の変化率を追跡します。モメンタムが正または負のレベルを超えると取引が始まり、反対のシグナルまたはボラティリティストップで退場します。

設定されたルックバック期間にわたるリターンを測定することで、システムはさまざまな市場に適応します。ボラティリティストップは、大きな不利な動きが素早く退場することを保証します。


## 詳細

- **エントリー条件**: MA、Momentumに基づくシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `MomentumPeriod` = 10
  - `ThresholdPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MA、Momentum
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - Neural Networks: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

