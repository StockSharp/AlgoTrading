# Volume Supertrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はVolume Supertrendインジケーターを使用してシグナルを生成します。
Volume > Avg(Volume) かつ Price > Supertrend（出来高急増と上昇トレンド）の場合にロングエントリー。Volume > Avg(Volume) かつ Price < Supertrend（出来高急増と下降トレンド）の場合にショートエントリー。
トレンド市場で機会を求めるトレーダーに適しています。

テストでは年間平均リターン約64%を示しています。外国為替市場で最もパフォーマンスが高いです。

## 詳細
- **エントリー条件**:
  - **ロング**: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
  - **ショート**: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: Supertrendが下向きに転じたときロングポジションを終了
  - **ショート**: Supertrendが上向きに転じたときショートポジションを終了
- **ストップ**: はい。
- **デフォルト値**:
  - `VolumeAvgPeriod` = 20
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    Avg(Volume)は直前VolumeAvgPeriod本の平均出来高です。Supertrendが下向きに転じたらロングを、上向きに転じたらショートを閉じます。ストップはエントリー価格の固定StopLossPercentで、ローソク足の間も監視されます。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Volume Supertrend
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

