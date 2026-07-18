# 輪ゴムグリッド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 4 Expert Advisor **RUBBERBANDS_2.mq4** の変換。
- ローソク足の代わりに最高買値/最高売値を使用して、現在の価格を中心に対称グリッドを実行します。
- 動作がヘッジされた MT4 実装と一致するように、長期エクスポージャーと短期エクスポージャーに対して個別の台帳を保持します。
- セッションレベルの損益管理と、元の入力と同じ手動の静止/停止モードを実装します。

## 取引ロジック
1. この戦略は `SubscribeLevel1()` をサブスクライブし、最良入札と最良売値のあらゆる変更に反応します。
2. 2 つの変動極値 (`_upperExtreme` / `_lowerExtreme`) は、最後のリセット以降に到達した最高売値と最低売値をキャプチャします。 `UseInitialValues` が true の場合はパラメータから初期化され、それ以外の場合は最初に受け取った売値が使用されます。
3. オープンな取引がなく、サーバー時間が 1 分の最初のティック (秒がゼロに等しい) に達すると、ストラテジーは市場での買いと市場での売りの両方を要求します。これは、ブックが空のときに売買フラグが 1 分ごとに設定される MT4 の動作を反映しています。
4. 売値が保存された高値を `GridStepPoints` ポイント上回るたびに、新しい売り注文が発行されます。保存された安値を同じ距離だけ下回るたびに、新しい買い注文がトリガーされます。トリガーごとに極値が現在のアスクに更新されるため、ラダーは価格に応じて「伸縮」します。
5. 同時にオープンする取引の総数 (ロングレッグとショートレッグの合計) は、`MaxTrades` によって制限されます。
6. 変動利益は現在のビッド/アスクから計算されます。ロング利益はビッドから平均ロング価格を引いたものを使用し、ショート利益は平均ショート価格からアスクを引いたものを使用します。ヘルパー `PriceToMoney` は、利用可能な場合、`PriceStep`/`StepPrice` を使用して、価格差をアカウント通貨に変換します。
7. 変動利益が `SessionTakeProfitPerLot * OrderVolume` に達し、`UseSessionTakeProfit` が有効になると、すべてのエクスポージャが平坦化されます。同様に、`UseSessionStopLoss` が有効な場合、`-SessionStopLossPerLot * OrderVolume` を下回る変動損失は完全な決済をトリガーします。
8. 手動フラグは、元の EA オプションを再現します。`CloseNow` はフラット スタートを強制し、`QuiesceMode` はフラットの間戦略をアイドル状態に保ち、`StopNow` は既存のポジションを妨げずに新しいエントリーを停止します。

## パラメーター
| パラメータ | 説明 |
|-----------|-------------|
| `OrderVolume` | すべての成行注文の出来高 (MT4 `Lots`)。 |
| `MaxTrades` | 同時にオープンする取引の最大数 (MT4 `maxcount`)。 |
| `GridStepPoints` | グリッド レイヤー間の価格ポイントの距離 (MT4 `pipstep`)。 |
| `QuiesceMode` | 有効にすると、戦略はフラットに 1 回待機します。これは `quiescenow` と同じです。 |
| `TriggerImmediateEntries` | 戦略の準備が整い次第、最初の売買を開始します (`donow`)。 |
| `StopNow` | 現在のポジションを維持したまま自動エントリーを一時停止します (`stopnow`)。 |
| `CloseNow` | 開始時に即時フラット化をリクエストします (`closenow`)。 |
| `UseSessionTakeProfit` & `SessionTakeProfitPerLot` | ロットごとのセッションレベルの変動利益目標。 |
| `UseSessionStopLoss` & `SessionStopLossPerLot` | ロットごとのセッションレベルの変動損失しきい値。 |
| `UseInitialValues`, `InitialMax`, `InitialMin` | 以前の極端な値 (`useinvalues`、`inmax`、`inmin`) を再利用するオプションの再起動サポート。 |

## 実装メモ
- プロジェクトのガイドラインに従って、すべての内部状態はタブでインデントされ、コレクションではなくフィールドに保存されます。
- 成行注文は `_activeBuyOrder` と `_activeSellOrder` を追跡することによって抑制されるため、前のリクエストが保留されている間に重複したリクエストが送信されることはありません。
- ヘッジ会計は `OnOwnTradeReceived` で実行され、ロングおよびショートの平均価格/出来高が個別に更新され、ストップ ロジックの変動利益に変換されます。
- `TryCloseAll()` は、MT4 `close1by1()` ルーチンを反映しており、両方の元帳がフラットになるまで反対の成行注文を送信し、その後、極値を最新のアスクにリセットします。
- この戦略は、高レベルの API 呼び出し (`SubscribeLevel1()`、`BuyMarket`、`SellMarket`) のみに依存し、リポジトリ ルールで要求されている直接のインジケーター アクセスを回避します。
