# Target EA Manager戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Target EA Manager戦略**は、MetaTrader エキスパート *TargetEA_v1.5* を StockSharp へ忠実に移植したものです。この戦略は自分では新規取引を開きません。代わりに、すでに戦略に属する注文の含み損益を常に監視し、必要であればユーザー定義のしきい値に達した時点でポジションを清算し、保留注文をキャンセルします。動作は元エキスパートの「バスケット」管理ロジックを再現します。買い注文と売り注文は独立して評価することも、単一の結合バスケットとして評価することもできます。

戦略は Level1 データ (最良 bid と ask) を購読し、ポジション決済と注文キャンセルには高レベル API を使用します。リアルタイムの bid/ask クォートは、オープンエクスポージャーの未実現利益指標へ変換されます。

## 主な機能
- **独立または結合バスケット** - `ManageBuySellOrders` により、ロング注文とショート注文を別々に扱うか、一緒に扱うかを選択します。
- **複数の目標タイプ** - しきい値は pips、ロットあたりの口座通貨、またはポートフォリオ残高の割合で表現でき、MQL 版の `TypeTargetUse` フラグに対応します。
- **両側トリガー** - 含み益 (`CloseInProfit`) と含み損 (`CloseInLoss`) へ反応する個別トグル。
- **保留注文の整理** - バスケットが閉じるたびに、買いおよび/または売りの保留注文を任意でキャンセルします。
- **高レベル操作** - 成行エグジットは `BuyMarket` / `SellMarket` で実行され、保留注文は戦略の注文コレクション経由でキャンセルされます。

## パラメーター
| パラメーター | 説明 |
|-----------|------|
| `ManageBuySellOrders` | `Separate` は 2 つのバスケット (ロングとショート) をエミュレートし、`Combined` は両側を統合します。 |
| `CloseBuyOrders` / `CloseSellOrders` | 対応する側の清算を有効にします。 |
| `DeleteBuyPendingPositions` / `DeleteSellPendingPositions` | バスケットが閉じた後、アクティブな保留注文をキャンセルします。 |
| `TypeTargetUse` | `Pips`、`CurrencyPerLot`、`PercentageOfBalance` は、オープン PnL に適用する測定方法を選択します。 |
| `CloseInProfit` / `CloseInLoss` | 利益または損失トリガーを有効にします。 |
| `TargetProfitInPips`, `TargetLossInPips` | pips 単位のしきい値。商品が `PriceStep` を提供する場合、pip 値は `priceDifference / PriceStep * (volume / VolumeStep)` として計算されます。 |
| `TargetProfitInCurrency`, `TargetLossInCurrency` | ロットあたりの含み益または含み損。比較前に現在数量を掛けます。 |
| `TargetProfitInPercentage`, `TargetLossInPercentage` | 決済前に到達する必要があるポートフォリオ残高の割合。元のエキスパートは生の含み益を `Balance ± Balance * Percentage / 100` と比較し、この移植版もその慣例をそのまま維持します。 |

## 動作
1. **状態追跡** - 実行済み取引は、内部のロングおよびショート数量合計と加重平均価格を更新します。そのため、ヘッジされたポジション (ロングとショートの両方) も正しく処理されます。
2. **PnL計算** - 各 Level1 更新で bid/ask 値を更新し、そこから両側の pips と通貨利益を計算します。
3. **目標評価** - 目標モードとバスケットモードに応じて、対応するしきい値を確認します。利益チェックでは設定目標以上の値が必要で、損失チェックでは以下の比較を使い、MQL ロジックと一致させます。
4. **バスケット清算** - 条件が満たされると、戦略は任意でその側の保留注文をキャンセルし、オープンエクスポージャーをフラット化するために必要な成行注文を送信します。

実装は意図的に追加コレクションやインジケーター保存を避け、元 EA と同じように StockSharp の高レベル API に依存します。
