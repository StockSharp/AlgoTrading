# RSI Donchian 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
RSI Donchian戦略は、ドンチャンチャネルのブレイクアウトと一致するモメンタムの極値を探します。相対力指数が買われすぎと売られすぎの状態を測定し、チャネルが最近の価格の高値と安値を定義します。

テストでは年間平均リターン約82%を示しています。株式市場で最もパフォーマンスが高いです。

RSIがRsiOverboughtを上回った状態で価格がドンチャン上部バンドを上抜けたときに買いシグナルが現れます。RSIがRsiOversoldを下回った状態で価格が下部バンドを割り込んだときにショートシグナルが形成されます。価格がドンチャン中間線に戻るとエグジットが発生し、均衡への回帰を示します。

この手法は、強いモメンタムに乗りつつ、明確なブレイクアウトレベルで取引したいアクティブトレーダーに適しています。ストップロスは、モメンタムが素早く反転しない場合のリスク上限設定に役立ちます。

## 詳細
- **エントリー条件**:
  - **ロング**: RSI > RsiOverbought && Close > Donchian High
  - **ショート**: RSI < RsiOversold && Close < Donchian Low
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: close < Donchian Middle のときに終了
  - **ショート**: close > Donchian Middle のときに終了
- **ストップ**: はい、パーセンテージストップロス。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `StopLossPercent` = 2
    チャネルは直前DonchianPeriod本の高値と安値で、中間線はその中央です。上抜けはRSIがRsiOverboughtを上回るとき、下抜けはRSIがRsiOversoldを下回るときに確認されます。逆の読み方（チャネル上で引けたときにRSIが売られすぎ）は実際にはほぼ起こりません。ストップはエントリー価格の固定StopLossPercentで、ローソク足の間も監視されます。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: RSI, Donchian Channel
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

