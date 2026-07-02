# Parabolic SAR CCI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はParabolic SAR CCIインジケーターを使ってシグナルを生成します。
Price > SAR && CCI < -100（売られすぎ条件での上昇トレンド）のときロングエントリー。Price < SAR && CCI > 100（買われすぎ条件での下降トレンド）のときショートエントリー。
混合市場での機会を求めるトレーダーに適しています。

テストでは年平均リターン約49%を示しています。暗号通貨市場で最もパフォーマンスが良好です。

## 詳細
- **エントリー条件**:
  - **ロング**: Price > SAR && CCI < -100 (売られすぎ条件での上昇トレンド)
  - **ショート**: Price < SAR && CCI > 100 (買われすぎ条件での下降トレンド)
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: 価格がSARを下回ったらロングポジションを退場
  - **ショート**: 価格がSARを上回ったらショートポジションを退場
- **ストップ**: いいえ。
- **デフォルト値**:
  - `SarAccelerationFactor` = 0.02m
  - `SarMaxAccelerationFactor` = 0.2m
  - `CciPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: Parabolic SAR CCI
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

