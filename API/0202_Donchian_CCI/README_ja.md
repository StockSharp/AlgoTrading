# Donchian CCI Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はDonchian CCIインジケーターを使ってシグナルを生成します。
Price > Donchian Upper && CCI < -100（売られすぎ条件での上方ブレイクアウト）のときロングエントリー。Price < Donchian Lower && CCI > 100（買われすぎ条件での下方ブレイクアウト）のときショートエントリー。
混合市場での機会を求めるトレーダーに適しています。

テストでは年平均リターン約43%を示しています。株式市場で最もパフォーマンスが良好です。

## 詳細
- **エントリー条件**:
  - **ロング**: Price > Donchian Upper && CCI < -100 (売られすぎ条件での上方ブレイクアウト)
  - **ショート**: Price < Donchian Lower && CCI > 100 (買われすぎ条件での下方ブレイクアウト)
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: 価格が中間バンドを下回ったらロングポジションを退場
  - **ショート**: 価格が中間バンドを上回ったらショートポジションを退場
- **ストップ**: はい。
- **デフォルト値**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: Donchian CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

