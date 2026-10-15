# Mad Trader戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Mad Traderは元のMQLエキスパート「madtrader-8.7」から変換されたトレンドフォロー戦略です。ATRとRSIインジケーターを組み合わせて、新興トレンド中の低ボラティリティ押し目を特定します。システムはATRが指定した閾値を下回りながらも上昇中であり、RSIが全体的な強気または弱気トレンドの中で増加するのを待ちます。これらの条件が揃い、ローソク足の実体が定義された範囲内にある場合、RSIが示す方向に成行注文を開きます。ポジションはトレーリングストップと、口座の資産が目標成長率に達したときにすべての取引を決済するバスケット利益メカニズムで保護されます。

## 詳細

- **エントリー条件**:
  - ATRが`MaxAtr`を下回り、前のATR値より大きい。
  - ローソク足の実体サイズが`MinCandle`と`MaxCandle`の間。
  - 取引時間が`[StartHour, EndHour)`内。
  - RSIトレンドが50を超え、現在のRSIが上昇中だが`RsiLowerLevel`を下回る → 買い。
  - RSIトレンドが50を下回り、現在のRSIが下落中だが`RsiUpperLevel`を上回る → 売り。
  - 取引間に最低`TradeInterval`の遅延を確保。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - トレーリングストップに到達。
  - バスケット利益目標に達した（`BasketProfit`または`BasketProfit * BasketBoost`）。
- **ストップ**: 価格ポイントで測定されるトレーリングストップ。
- **デフォルト値**:
  - `AtrPeriod` = 14
  - `RsiPeriod` = 14
  - `TrendBars` = 60
  - `MinCandle` = 5
  - `MaxCandle` = 10
  - `MaxAtr` = 10
  - `RsiUpperLevel` = 50
  - `RsiLowerLevel` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `TradeInterval` = 30分
  - `TrailingStop` = 7
  - `BasketProfit` = 1.05
  - `BasketBoost` = 1.1
  - `RefreshHours` = 24
  - `ExponentialGrowth` = 0.01
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: ATR, RSI
  - ストップ: トレーリング
  - 複雑さ: 中程度
  - 時間軸: 短期（5分足）
  - リスクレベル: 中
