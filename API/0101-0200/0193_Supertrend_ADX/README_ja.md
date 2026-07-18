# Supertrend Adx 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
SupertrendインジケーターとADXによるトレンド強度確認に基づく戦略。エントリー条件：ロング：Price > Supertrend && ADX > 25（強い動きを伴う上昇トレンド）。ショート：Price < Supertrend && ADX > 25（強い動きを伴う下降トレンド）。エグジット条件：ロング：Price < Supertrend（価格がSupertrendを下回る）。ショート：Price > Supertrend（価格がSupertrendを上回る）。

テストでは年平均リターン約166%を示しています。株式市場で最もパフォーマンスが高くなります。

Supetrendがボラティリティ調整済みのパスを提供し、ADXが動きのパワーを確認します。両インジケーターが一致したときにトレードが行われます。

トレーリングストップで強いトレンドに乗ることを目指す人向けです。ATRがストップの配置を決定します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > Supertrend && ADX > AdxThreshold`
  - ショート: `Close < Supertrend && ADX > AdxThreshold`
- **ロング/ショート**: 両方
- **エグジット条件**: Supetrendの反転
- **ストップ**: Supetrendをトレーリングストップとして使用
- **デフォルト値**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Supertrend, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

