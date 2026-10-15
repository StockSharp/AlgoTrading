# Exp トレンド強度指数戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderエキスパート **Exp_Trend_Intensity_Index** のStockSharp変換版です。設定可能な時間軸の完成したローソク足で取引し、トレンド強度指数（TII）を使用してモメンタムが極端な強気または弱気ゾーンを出るタイミングを検出します。インジケーターが上部ゾーンから外に移行すると、アルゴリズムはショートをクローズし、新しいロングを開始できます。インジケーターが下部ゾーンを出ると、アルゴリズムはロングをクローズし、新しいショートを開始できます。

## インジケーターの構築方法

1. 価格ソースを選択します（close、open、加重バリアント、トレンドフォロー価格など）。
2. その価格ストリームを最初の移動平均で平滑化します（`PriceMaMethod`、`PriceMaLength`）。
3. 価格と平滑化値の差を正のフローと負のフローに分割します。
4. 正のフローと負のフローを独立して2番目の移動平均で平滑化します（`SmoothingMethod`、`SmoothingLength`）。
5. トレンド強度指数を計算します：`TII = 100 * Positive / (Positive + Negative)`。
6. 結果を `HighLevel` と `LowLevel` の閾値と比較して色の状態を割り当てます：高ゾーン（`0`）、ニュートラル（`1`）、または低ゾーン（`2`）。

実装はStockSharpの移動平均（単純、指数、平滑化、加重）を使用します。元のMQLライブラリの高度な平滑化タイプはこのポートでは利用できません。

## 取引ロジック

* シグナルはローソク足が完全にクローズした場合にのみ処理されます（`CandleStates.Finished`）。
* `SignalBar`パラメーターはどの完成したバーを分析するかを定義します（デフォルト：1バー前）。戦略はMQLコードのダブルバッファルックアップに一致させて、その直前のバーも検査します。
* 古いバーが高ゾーンに属する場合（`color == 0`）：
  * `EnableSellExits`が真の場合、ショートポジションをクローズします。
  * より最近のバーが高ゾーンを出て `EnableBuyEntries` が真の場合、ロングポジションを開くか反転します。
* 古いバーが低ゾーンに属する場合（`color == 2`）：
  * `EnableBuyExits`が真の場合、ロングポジションをクローズします。
  * より最近のバーが低ゾーンを出て `EnableSellEntries` が真の場合、ショートポジションを開くか反転します。
* 注文は `BuyMarket` と `SellMarket` で送信されます。ポジションリバーサルは現在のポジションボリュームに設定された `Volume` プロパティを加算して使用します。
* オプションのストップロスとテイクプロフィット保護（価格単位）は `StopLossPoints` と `TakeProfitPoints` を通じて設定され、`StartProtection` で実装されます。

## パラメーター

| パラメーター | 説明 |
| --- | --- |
| `CandleType` | インジケーター計算と取引に使用する時間軸。 |
| `PriceMaMethod`, `PriceMaLength` | 基本価格ストリームに適用される移動平均タイプと期間。 |
| `SmoothingMethod`, `SmoothingLength` | 正のフローと負のフローに適用される移動平均タイプと期間。 |
| `AppliedPrice` | インジケーターの価格ソース（close、open、median、トレンドフォローバリアント、Demarなど）。 |
| `HighLevel`, `LowLevel` | 強気と弱気ゾーンを定義する上限と下限の閾値。 |
| `SignalBar` | シグナル確認のために振り返る完成したバーの数。 |
| `EnableBuyEntries`, `EnableSellEntries` | ロング/ショートポジションを開くことを許可するトグル。 |
| `EnableBuyExits`, `EnableSellExits` | インジケーターが反転したときの自動エグジットを許可するトグル。 |
| `StopLossPoints`, `TakeProfitPoints` | `StartProtection`のための価格単位で表されたオプションの保護距離。 |

## 元のMQLエキスパートとの違い

* マネー管理オプション（`MM`、`MMMode`、`Deviation`）はStockSharpの標準ボリュームプロパティと注文執行に置き換えられます。スリッページ管理は複製されません。
* StockSharpで利用可能な移動平均タイプ（単純、指数、平滑化、加重）のみがサポートされます。
* StockSharpのインジケーターは同等のコントロールを公開しないため、MQLインジケーターのフェーズパラメーターは省略されます。
* 注文は完成したローソク足でシグナルが確認された直後に実行されます。次のバー開始のための明示的なスケジューリングはありません。

これらの変更により、取引のアイデアはそのままに、StockSharpの高レベル戦略ガイドラインに従っています。
