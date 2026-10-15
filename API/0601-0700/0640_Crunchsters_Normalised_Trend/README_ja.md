# Crunchsters 正規化トレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

リターンを正規化し、累積正規化価格にHull Moving Averageを適用する戦略です。
正規化価格がHMAを上抜けたときにロングエントリーし、下抜けたときにショートエントリーします。

テストでは年平均リターン約105%を示しています。暗号資産市場で最もよいパフォーマンスを発揮します。

正規化リターンにより、直近のボラティリティに対して価格をスケーリングできます。ATRベースのストップでリスクを管理します。

## 詳細

- **エントリー条件**:
  - ロング: `nPrice`が`HMA`を上抜け
  - ショート: `nPrice`が`HMA`を下抜け
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のクロスオーバーまたはATRストップ
- **ストップ**: `StopMultiple`を使用したATRベース
- **デフォルト値**:
  - `NormPeriod` = 14
  - `HmaPeriod` = 100
  - `HmaOffset` = 0
  - `StopMultiple` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Hull Moving Average, Standard Deviation, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
