# 戦略 Basket Close Utility
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Basket Close Utility 戦略は、MetaTrader のエキスパート「Basket Close 2」の動作を反映しています。接続されたポートフォリオ内のすべてのオープンポジションの変動損益を継続的に監視します。 When either a configurable profit objective or a loss limit is reached, the strategy sends market orders to flatten **all** exposures in every instrument involved. Optionally, it can automatically open a small test position whenever the book is flat, which is useful inside backtests for validating that the protection logic works as expected.

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `LossMode` | Chooses whether the loss guard compares percentages or currency values. |
| `LossPercentage` | `LossMode` が `Percentage` のときに損失出口をトリガーする負の割合のドローダウン (絶対値で表される)。 |
| `LossCurrency` | `LossMode` が `Currency` の場合に決済をトリガーする口座通貨の変動損失。 |
| `ProfitMode` | 利益目標がパーセンテージと通貨値のどちらを比較するかを選択します。 |
| `ProfitPercentage` | `ProfitMode` が `Percentage` のときにすべてのポジションを決済する割合のゲイン。 |
| `ProfitCurrency` | Floating profit in account currency that closes all positions when `ProfitMode` is `Currency`. |
| `CandleType` | 変動損益の定期的なチェックをトリガーするために使用される時間枠。 |
| `EnableTestOrders` | 有効にすると、オープンなポジションがない場合は常に、戦略は単一の成行買い注文を送信します。 |
| `TestOrderVolume` | オプションのテスト注文がアクティブな場合に使用される取引サイズ。 |

## 取引ロジック
1. Subscribe to the configured candle series and run the evaluation only when a candle is fully finished, matching the behaviour of the original EA that works on closed bars.
2. すべてのオープンポジションの変動損益を集計します。ポートフォリオ オブジェクトが合計変動利益を公開する場合、それが使用されます。それ以外の場合、戦略は各ポジションの損益を合計します。
3. Compute the percentage change relative to the current account balance captured at start-up.
4. 変動損益が設定された制限に違反したときに損失ルーチンをトリガーします。変動損益または利益率が利益目標に達したときに、利益ルーチンをトリガーします。
5. 一度トリガーされると、ポートフォリオ全体のすべてのオープンポジションがクローズされるまで成行注文を送信し続けます。 This includes the main security as well as positions opened by child strategies.
6. オプションで、ブックがフラットになった後に成行注文を送信して (テスト用に) エクスポージャーを再開します。

## 注意事項
- MetaTrader の専門家は、グラフ上にテキスト情報を表示しました。 In StockSharp the important figures are logged through `LogInfo` instead.
- Swap and commission adjustments from the original script are implicitly included inside the floating PnL reported by the portfolio or individual positions.
- 割合のしきい値には、戦略の開始時に取得されたアカウント残高が使用されます。資本ベースが大幅に変化する場合は、長時間セッションを実行するときに制限を調整します。
- When the optional test order is enabled, the helper order is reissued whenever the previous exposure has been closed by the profit or loss guard.
