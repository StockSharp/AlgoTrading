# RsiBoosterStrategy戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

`RsiBoosterStrategy` は、MetaTrader のエキスパートアドバイザー *RSI booster* を StockSharp に移植したものです。この戦略は、現在のローソク足で計算した高速 RSI 値と、前のローソク足を使う遅延 RSI を比較します。差がユーザー定義の比率を超えると市場ポジションを開き、その後は固定ストップ、take-profit 目標、任意の trailing stop、損失回復用の逆方向注文チェーンで取引を管理します。

この戦略は StockSharp の高レベル API 上に構築されています。単一のローソク足系列を購読し、組み込みの `RelativeStrengthIndex` インジケーターを使用し、すべての入力を Designer 内で最適化できるように戦略パラメーターシステムを利用します。

## 取引ロジック

1. 確定した各ローソク足で 2 つの RSI インジケーターを計算します。
   * 高速 RSI は `FirstRsiPeriod` と `FirstRsiPrice` を使い、直近のローソク足を読み取ります。
   * 遅延 RSI は `SecondRsiPeriod` と `SecondRsiPrice` を使いますが、戦略は前回値を保持するため、1 バー遅れとして機能します。
2. `fast RSI - delayed RSI` が `Ratio` より大きい場合、ロングポジションが開いていなければ買います。差が `-Ratio` を下回る場合、ショートポジションが開いていなければ売ります。
3. `OnlyOnePositionPerBar` により、同じローソク足のタイムスタンプでは方向ごとに最大 1 回だけエントリーします。
4. 各ローソク足の後、戦略は stop-loss、take-profit、trailing のルールを評価します。いずれかの条件が発動すると、ポジションは直ちに決済されます。
5. ポジションがマイナスの実現 PnL で決済された場合、任意の回復ロジックにより、同じ数量で逆方向のポジションに入ることができます。連鎖する回復取引の数は `ReturnOrdersMax` で制限されます。

## リスク管理

* **Stop-loss** - `StopLossPips` により、商品のポイント単位で表されます。価格がストップ水準を交差するとポジションを決済します。
* **Take-profit** - `TakeProfitPips` により、商品のポイント単位で表されます。
* **Trailing stop** - `TrailingStopPips` で有効にすると、利益が設定距離を超えた時点でストップが追随を開始します。`TrailingStepPips` は trailing 水準を動かす前に必要な最小改善幅を定義します。
* **リターン注文** - `ReturnOrderEnabled` が `true` のときに有効になります。損失取引の後、戦略は反対方向の成行注文を即座に開き、発行した回復注文数を数え続けます。

## パラメーター

| パラメーター | 説明 |
|-----------|------|
| `Volume` | 各成行注文に使用する取引数量 (ロットまたは契約)。 |
| `Ratio` | ポジションを開くために必要な最小 RSI 差。 |
| `StopLossPips` | 商品ポイント単位の stop-loss 距離。 |
| `TakeProfitPips` | 商品ポイント単位の take-profit 距離。 |
| `TrailingStopPips` | 商品ポイント単位の trailing stop 距離。 |
| `TrailingStepPips` | trailing stop を動かす前に必要な最小改善幅。 |
| `OnlyOnePositionPerBar` | 同じローソク足内で複数回エントリーすることを防ぎます。 |
| `ReturnOrderEnabled` | 逆方向注文による回復ロジックを有効にします。 |
| `ReturnOrdersMax` | 連続する回復注文の最大数。 |
| `FirstRsiPeriod` | 高速 RSI の期間。 |
| `FirstRsiPrice` | 高速 RSI の価格ソース (MetaTrader の適用価格モードに対応)。 |
| `SecondRsiPeriod` | 遅延 RSI の期間。 |
| `SecondRsiPrice` | 遅延 RSI の価格ソース (MetaTrader の適用価格モードに対応)。 |
| `CandleType` | 分析に使用するローソク足系列。 |

## 注意事項

* 価格ステップの変換では、利用可能な場合に商品の `PriceStep` を尊重します。商品が価格ステップを提供しない場合は、代替値として `0.0001` を使用します。
* 回復チェーンのカウンターは、利益の出た取引が発生したとき、または設定された回復注文の最大数に達したときにリセットされます。
* 戦略は、実行済み取引と並べてすばやく視覚確認できるように、両方の RSI インジケーターをチャート領域に描画します。
