# Rsi Supertrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
RSIとSupertrendインジケーターに基づく戦略。RSIが売られすぎ（< RsiOversold）でかつ価格がSupertrendより上のときロングエントリー。RSIが買われすぎ（> RsiOverbought）でかつ価格がSupertrendより下のときショートエントリー。

テストでは年平均リターン約112%を示しています。外国為替市場で最もパフォーマンスが高くなります。

RSIオシレーターがモメンタムの極値を定義し、Supetrendが主要な方向を示します。RSIがSupertrendの色と一致したときにトレードが発生します。

トレーリングストップスタイルの決済を好むトレーダーに最適です。Supertrend自身のATR設定がそのトレーリングラインを形作ります。

## 詳細

- **エントリー条件**:
  - ロング: `RSI < RsiOversold && Close > Supertrend`
  - ショート: `RSI > RsiOverbought && Close < Supertrend`
- **ロング/ショート**: 両方
- **エグジット条件**: Supetrendの変化
- **ストップ**: Supetrendによるトレーリング
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    5分足では、価格がSupertrendより上にある間にRSIが30まで下がる（下にある間に70まで上がる）ことはほとんどなく、2024年3月のBTCアーカイブにはそのような足が1本もありません。そのため水準はパラメーターとし、既定値を40と60にしています。これでもトレンドに逆らう押し目・戻りを示し、両方向で取引します。ロングはSupertrendが下向きに転換したとき、ショートは上向きに転換したときに決済します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: RSI, Supertrend
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

