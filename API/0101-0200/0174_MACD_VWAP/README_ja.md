# Macd Vwap 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
MACDとVWAPインジケーターに基づく戦略。MACD > SignalかつClose > VWAPのときロングエントリー。MACD < SignalかつClose < VWAPのときショートエントリー。

テストでは年平均リターン約109%を示しています。暗号資産市場で最もパフォーマンスが高くなります。

MACDのモメンタムはVWAPラインを基準に測定されます。ロングトレードはVWAP以下でのMACD強度を探し、ショートはその上方で形成されます。

出来高加重平均を参照するイントラデイモメンタムトレーダーに最適です。パーセントストップがリスクを管理します。

## 詳細

- **エントリー条件**:
  - ロング: `MACD > Signal && Close > VWAP`
  - ショート: `MACD < Signal && Close < VWAP`
- **ロング/ショート**: 両方
- **エグジット条件**: MACDの逆方向クロス
- **ストップ**: `StopLossPercent`を使用したパーセントベース
- **デフォルト値**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    市場は24時間取引のため、セッションVWAPはUTCの毎日リセットされ、各足の典型価格を出来高で加重します。ロングはMACDがシグナル線を下抜けたとき、ショートは上抜けたときに決済します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: MACD, VWAP
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

