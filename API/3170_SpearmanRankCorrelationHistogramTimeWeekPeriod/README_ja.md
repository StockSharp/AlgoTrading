# Spearman Rank Correlation Histogram Time Window戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はStockSharpの高レベルAPIでMetaTraderエキスパート**Exp_SpearmanRankCorrelation_Histogram_TimeWeekPeriod**を再現します。単一のローソク足ストリーム（デフォルト：4時間バー）をサブスクライブし、元のMQLインジケーターで公開されているSpearmanランク相関ヒストグラムを評価します。ヒストグラムの色は短期トレンドが強気（ゼロより上の値）か弱気（ゼロより下の値）かを決定します。専用の取引ウィンドウは、ソースコードの`TimeTrade`コントロールを反映して、設定可能な曜日/時間範囲の間のアクティビティを維持します。

## トレードロジック
1. **インジケーター計算**
   - 各完成ローソク足で戦略は終値を保存し、`RangeLength`の終値にわたってSpearmanランク相関を計算します。
   - ヒストグラムの色はインジケーターと全く同じように割り当てられます：相関が`HighLevel`より上のとき`4`、`0`と`HighLevel`の間のとき`3`、`LowLevel`と`0`の間のとき`1`、`LowLevel`より下のとき`0`、正確にゼロのとき`2`。
   - シグナルはクローズしたバー番号`SignalBar`で評価されます（デフォルト：たった今クローズしたバー）。前のクローズしたバーはカラー遷移を検出するために使用されます。

2. **取引モード** – `TradeMode`パラメーターは色の解釈方法を制御します：
   - **Mode1** – 色が`3`より下にあった後`2`より上にジャンプしたらロングを開く；色が`1`より上にあった後`2`より下に落ちたらショートを開く。各強気色はショートクローズも要求し、各弱気色はロングクローズを要求します。
   - **Mode2** – 色`4`でロングを開く（`4`より下からの遷移）、色`0`でショートを開く（`0`より上からの遷移）。`2`より大きい色はショートをクローズ；`2`より小さい色はロングをクローズ。
   - **Mode3** – 色`4`でロングを開き同時にショートをクローズ；色`0`でショートを開き同時にロングをクローズ。
   - 成功したエントリー後、戦略はローソク足の長さに等しいクールダウンを適用します（同じ方向の次の注文はMetaTraderで次のバーがクローズするまで延期されます）。

3. **資金管理と注文サイズ**
   - `MoneyManagement`と`MarginMode`を組み合わせてエクイティまたはリスクの割合を注文ボリュームに変換します。正の値は元の資金管理ルールに従い、ゼロは戦略の`Volume`にフォールバックし、負の数は固定ロットサイズとして解釈されます。
   - リスクベースのモード（`LossFreeMargin`、`LossBalance`）は正の`StopLossPoints`を必要とします。ストップがゼロの場合、EAが取引を拒否するのと同様に戦略は`Volume`に戻ります。

4. **リスク管理**
   - `StopLossPoints`と`TakeProfitPoints`は`Security.PriceStep`を使用して価格レベルに変換されます。エグジットは各完成ローソク足でローソク足の高値/安値を使用して確認され、レベルがタッチされると全オープンポジションがフラットに戻されます。
   - `DeviationPoints`はUI完全性のために保持されます；StockSharpの市場注文はこの値を無視します。

5. **週次取引ウィンドウ**
   - `TimeTrade`が`true`のとき、現在の時刻は（`StartDay`、`StartHour`、`StartMinute`、`StartSecond`）と（`EndDay`、`EndHour`、`EndMinute`、`EndSecond`）の間でなければなりません。そのウィンドウ外では、戦略インストゥルメントのすべてのポジションが即座にクローズされ、元の緊急エグジットと一致します。
   - 実装は`StartDay`が`EndDay`より遅くないと仮定します。重複セッション（例：金曜日→月曜日）の場合はパラメーターを適切に調整してください。

6. **その他の動作**
   - シグナルが生成される前に少なくとも`RangeLength + SignalBar + 1`の完成したローソク足が利用可能である必要があります。
   - `Direction`はMQLインジケーターの予約済みスイッチです；パラメーター同等性のために保持されていますが、このポートでは効果がありません。

## パラメーター
| 名前 | 説明 | デフォルト値 |
| --- | --- | --- |
| `MoneyManagement` | ポジションサイジングに使用する資本の割合または固定ロットサイズ。 | `0.1` |
| `MarginMode` | `MoneyManagement`の解釈（`FreeMargin`、`Balance`、`LossFreeMargin`、`LossBalance`、`Lot`）。 | `Lot` |
| `StopLossPoints` | 価格ポイントでのストップロス距離。 | `1000` |
| `TakeProfitPoints` | 価格ポイントでのテイクプロフィット距離。 | `2000` |
| `DeviationPoints` | ポイントでの情報的スリッページ許容範囲。 | `10` |
| `BuyOpen` / `SellOpen` | ロングまたはショートポジションの開設を有効にする。 | `true` |
| `BuyClose` / `SellClose` | シグナルでのロングまたはショートポジションのクローズを許可する。 | `true` |
| `TradeMode` | ヒストグラム解釈モード（`Mode1`、`Mode2`、`Mode3`）。 | `Mode1` |
| `TimeTrade` | 週次取引ウィンドウを切り替える。 | `true` |
| `StartDay`, `StartHour`, `StartMinute`, `StartSecond` | ウィンドウの開始（曜日と時刻）。 | `火曜日`, `8`, `0`, `0` |
| `EndDay`, `EndHour`, `EndMinute`, `EndSecond` | ウィンドウの終了（曜日と時刻）。 | `金曜日`, `20`, `59`, `40` |
| `CandleType` | 処理されるローソク足の時間軸。 | `H4` |
| `RangeLength` | Spearman相関に使用する終値の数。 | `14` |
| `MaxRange` | 許可される最大`RangeLength`（安全ガード）。 | `30` |
| `Direction` | 予約済みインジケーターフラグ、ポートでは効果なし。 | `true` |
| `HighLevel`, `LowLevel` | 上部と下部のヒストグラム閾値。 | `0.5`, `-0.5` |
| `SignalBar` | カラーバッファを読む際に遡るクローズバーの数。 | `1` |

その他すべての戦略設定（ポートフォリオ選択、証券割り当て、ベース`Volume`、リスクルール）はStockSharpの標準ワークフローに従います。
