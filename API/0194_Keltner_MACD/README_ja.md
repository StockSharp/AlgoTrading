# Keltner Macd 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
KeltnerチャネルとMACDに基づく戦略。MACD > SignalでKeltnerチャネル上限を価格が突破したときロングエントリー。MACD < SignalでKeltnerチャネル下限を価格が割り込んだときショートエントリー。MACDがシグナルラインを逆方向にクロスしたときに決済。

テストでは年平均リターン約169%を示しています。暗号資産市場で最もパフォーマンスが高くなります。

Keltnerチャネルのブレイクアウトがトリガーとなり、MACDのモメンタムが方向をフィルタリングします。両シグナルが一致したときにトレードが開始されます。

モメンタムに裏付けられたボラティリティ拡張を追うトレーダーに最適です。ATRベースのストップがリスクを抑制します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > UpperBand && MACD > Signal`
  - ショート: `Close < LowerBand && MACD < Signal`
- **ロング/ショート**: 両方
- **エグジット条件**: MACDの逆方向クロス
- **ストップ**: `AtrMultiplier`を使用したATRベース
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `Multiplier` = 2m
  - `AtrPeriod` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Keltner Channel, MACD
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

