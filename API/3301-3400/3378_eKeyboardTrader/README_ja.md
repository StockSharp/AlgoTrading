# eKeyboardTrader 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、StockSharp の高レベル API を使用して、MetaTrader「eKeyboardTrader」エキスパート アドバイザの動作を複製します。元のスクリプトは、手動成行注文を送信するためのキーボード ショートカットをリッスンし、チャート上にヘルパー テキストを直接表示しました。 StockSharp バージョンでは、インタラクティブな入力は戦略パラメーターとして公開されますが、実行ロジック、安全性チェック、注文保護機能は MQL の実装に忠実なままです。

## 取引ロジック
1. **レベル 1 サブスクリプション** – この戦略はレベル 1 の市場データをサブスクライブして、最新の最高買値と売値を受け取ります。 These quotes are required before a manual request can be executed, mimicking the MetaTrader dependency on current tick data.
2. **手動コマンド** – 3 つのブール値パラメータ (`BuyRequest`、`SellRequest`、`CloseRequest`) は、元のキーボード ショートカット (B、S、および C) を表します。いずれかのパラメータが `true` に設定されている場合、戦略は対応する市場アクションを実行し、ただちにフラグをリセットします。
3. **レート制限** – 1 秒間のクールダウンは、MQL バージョンに実装されているタイマー チェックと同じ、偶発的な二重送信を防ぎます。 Requests raised during the cooldown wait for the next processing cycle.
4. **注文保護** – MetaTrader ポイントで表されるオプションのストップロスとテイクプロフィットの距離は、`Security.PriceStep` を使用して絶対価格に変換されます。少なくとも 1 つの保護距離が設定されている場合、この戦略により StockSharp の組み込み `StartProtection` ロジックが有効になり、すべての手動入力が設定された保護命令を自動的に受け取るようになります。
5. **スリッページ認識** – `SlippagePoints` パラメータは互換性のために保存されており、手動注文が送信されるたびにログに記載され、エキスパートアドバイザーによって表示される情報コメントをエミュレートします。

## パラメーター
| パラメータ | 説明 |
|-----------|-------------|
| `OrderVolume` | 手動成行注文の基本量。 |
| `StopLossPoints` | エントリー価格からプロテクティブストップまでの距離 (MetaTrader ポイント)。無効にするには、`0` に設定します。 |
| `TakeProfitPoints` | エントリー価格から保護ターゲットまでの距離 (MetaTrader ポイント)。無効にするには、`0` に設定します。 |
| `SlippagePoints` | 各手動注文のログに表示される情報スリッページ許容値。 |
| `BuyRequest` | 成行買い注文を送信するには、`true` に設定します (処理後に自動リセット)。 |
| `SellRequest` | Set to `true` to send a market sell order (auto-resets after processing). |
| `CloseRequest` | ネットポジションを市場価格でフラット化するには、`true` に設定します (処理後に自動リセット)。 |

## MQL バージョンとの違い
- The on-chart text prompts and sound notifications are not reproduced.代わりに、ログメッセージに実行されたアクションが記録されます。
- 保護注文は、StockSharp の `StartProtection` ヘルパーによって管理されます。このヘルパーは、しきい値に達したときに、個々の MetaTrader チケットを変更するのではなく、成行注文を送信します。
- キーボード入力はパラメータの切り替えに置き換えられます。戦略をホストする UI は、ユーザーの操作 (キーボード、ボタン、スクリプト) をこれらのパラメーターにマップできます。
- MetaTrader 取引リクエストの診断は、変換を軽量に保つためにログ ステートメントに凝縮されています。

## 使用上の注意
- 戦略を開始する前に、`Security` と `Portfolio` の両方を割り当ててください。これらのチェックは、エキスパートアドバイザーからの初期化条件を反映します。
- The manual command flags are evaluated when new Level1 data arrives.静かな市場では、アクションは次に利用可能な相場に基づいて実行されます。
- 戦略の実行中に `StopLossPoints` または `TakeProfitPoints` を調整するには、戦略を再起動して保護モジュールを再構成し、元のスクリプトのセッションごとに 1 回の保護設定と一致させる必要があります。
