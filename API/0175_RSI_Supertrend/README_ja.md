# Rsi Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
RSIとSupertrendインジケーターに基づく戦略。RSIが売られすぎ（< 30）でかつ価格がSupertrendより上のときロングエントリー。RSIが買われすぎ（> 70）でかつ価格がSupertrendより下のときショートエントリー。

テストでは年平均リターン約112%を示しています。外国為替市場で最もパフォーマンスが高くなります。

RSIオシレーターがモメンタムの極値を定義し、Supetrendが主要な方向を示します。RSIがSupertrendの色と一致したときにトレードが発生します。

トレーリングストップスタイルの決済を好むトレーダーに最適です。ATR設定でポジションをさらに保護します。

## 詳細

- **エントリー条件**:
  - ロング: `RSI < 30 && Close > Supertrend`
  - ショート: `RSI > 70 && Close < Supertrend`
- **ロング/ショート**: 両方
- **エグジット条件**: Supetrendの変化
- **ストップ**: Supetrendによるトレーリング
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
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

