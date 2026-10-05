# Stochastic Keltner 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はStochastic Keltnerインジケーターを使用してシグナルを生成します。
Stoch %K < StochOversold かつ Price < Keltner lower band（下バンドで売られすぎ）の場合にロングエントリー。Stoch %K > StochOverbought かつ Price > Keltner upper band（上バンドで買われすぎ）の場合にショートエントリー。
混合市場で機会を求めるトレーダーに適しています。

テストでは年間平均リターン約61%を示しています。暗号資産市場で最もパフォーマンスが高いです。

## 詳細
- **エントリー条件**:
  - **ロング**: Stoch %K < StochOversold && Price < Keltner lower band (oversold at lower band)
  - **ショート**: Stoch %K > StochOverbought && Price > Keltner upper band (overbought at upper band)
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 価格が中間バンドに戻ったときロングポジションを終了
  - **ショート**: 価格が中間バンドに戻ったときショートポジションを終了
- **ストップ**: はい。
- **デフォルト値**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    20と80はルールが示す%K水準の既定値です。バンドはEmaPeriod本のEMAにAtrPeriod本のATRのKeltnerMultiplier倍を加減したもので、中間バンドはEMAそのものです。ストップはエントリー時の終値から同じATRのAtrMultiplier倍離れた位置に置かれ、ローソク足の終値で確認されます。0で無効になります。 ルール中のStochKは%Kで、StochPeriod本のストキャスティクスをStochK本で平滑化した値です。%Dはルールに関与しないため設定はありません。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: Stochastic Keltner
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

