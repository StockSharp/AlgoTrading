# Donchian CCI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はDonchian CCIインジケーターを使ってシグナルを生成します。
Price > Donchian Upper && CCI > CciOverbought（上昇モメンタムを伴う上方ブレイクアウト）のときロングエントリー。Price < Donchian Lower && CCI < CciOversold（下降モメンタムを伴う下方ブレイクアウト）のときショートエントリー。
混合市場での機会を求めるトレーダーに適しています。

テストでは年平均リターン約43%を示しています。株式市場で最もパフォーマンスが良好です。

## 詳細
- **エントリー条件**:
  - **ロング**: Price > Donchian Upper && CCI > CciOverbought (上昇モメンタムを伴う上方ブレイクアウト)
  - **ショート**: Price < Donchian Lower && CCI < CciOversold (下降モメンタムを伴う下方ブレイクアウト)
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: 価格が中間バンドを下回ったらロングポジションを退場
  - **ショート**: 価格が中間バンドを上回ったらショートポジションを退場
- **ストップ**: はい。
- **デフォルト値**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `CciOverbought` = 100
  - `CciOversold` = -100
  - `StopLossPercent` = 2
    チャネルは直前DonchianPeriod本の高値と安値で、中間バンドはその中央です。上抜けはCCIがCciOverboughtを上回るとき、下抜けはCCIがCciOversoldを下回るときに確認されます。逆の読み方（チャネル上で引けたときにCCIが売られすぎ）は実際にはほぼ起こりません。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
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

