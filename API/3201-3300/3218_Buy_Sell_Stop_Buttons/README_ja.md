# Buy Sell Stop Buttons戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 4の「Buy Sell Stop Buttons」エキスパートアドバイザーをStockSharp内に再現します。
- チャートボタンをエミュレートする3つの手動パラメーター（`BuyRequest`、`SellRequest`、`CloseRequest`）を提供します。
- 固定金額テイクプロフィット、パーセントテイクプロフィット、エクイティトレーリングロック、ブレークイーブン、ピップトレーリングストップという同じ資金管理ヘルパーを実装します。
- 完成したバーの管理ルールを評価するためのハートビートとして、1分足サブスクリプションを純粋に使用します。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `OrderLots` | 手動エントリーが要求されたときに使用される基本ロットサイズ。`Lots` extern入力（デフォルト`0.01`）を反映。 |
| `NumberOfTrades` | リクエストごとに送信されるチケット数。C#ポートはボリュームを単一の成行注文に統合します。 |
| `UseTakeProfitInMoney` / `TakeProfitInMoney` | 到達時にすべての取引を閉じる直接金額ターゲットを有効化および設定。 |
| `UseTakeProfitPercent` / `TakeProfitPercent` | エクイティパーセントターゲットを有効化および設定。戦略は口座残高を近似するために`Portfolio.CurrentValue`を使用。 |
| `EnableTrailing`, `TrailingProfitMoney`, `TrailingLossMoney` | エクイティトレーリングブロックを設定：利益が`TrailingProfitMoney`を超えたら最高値を追跡し、利益が`TrailingLossMoney`下落したら全取引を閉じる。 |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | ポジションが設定済みピップ距離を稼いだ後、ストップをブレークイーブン＋オフセットに移動。 |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | StockSharpでピップ距離に変換されたチケット管理設定。 |
| `CandleType` | ハートビートを駆動するローソク足シリーズ（デフォルトは1分足）。 |
| `BuyRequest`, `SellRequest`, `CloseRequest` | 元のチャートボタンを置き換える手動コマンド。アクションが成功した後、フラグは自動的にリセットされます。 |

## トレードロジック
1. `OnStarted`は設定されたローソク足シリーズにサブスクライブし、基本`Volume`を設定し、組み込みポジション保護を有効化します。
2. 各完成したローソク足は以下のワークフローをトリガーします：
   - 手動コマンドが評価される：買いと売りは`OrderLots * NumberOfTrades`のボリュームで成行注文を送信し、オプションで反対ポジションをオフセット；決済要求は戦略をフラット化する。
   - 金額ターゲットが順番にチェックされる：固定金額、エクイティのパーセント、次にエクイティトレーリングロック。
   - ブレークイーブンとピップトレーリングストップは平均エントリー価格に基づいて内部ストップレベルを調整する。
   - 静的ストップロス/テイクプロフィット距離が適用される。
   - オプションのボリンジャーバンド出口は、上バンドに触れるロングまたは下バンドに触れるショートを閉じる（20期間、幅2）。
3. 未実現利益は利用可能な場合`Security.PriceStep`/`Security.StepPrice`で計算され；そうでない場合は価格差フォールバックが使用される。

## MQLバージョンとの違い
- MetaTraderはヘッジポジションを許可していた；StockSharpは露出を純計するので、買い/売り要求は最初に反対ポジションを中立化する。
- 月次MACDベースの出口（`Close_BUY`/`Close_SELL`）は、元のスクリプトで呼び出されなかったため存在しない。
- `MaximumRisk`/`DecreaseFactor`による自動ボリュームスケーリングは明示的な`OrderLots`パラメーターに置き換えられる。MQLヘルパーはこのポートでは利用できない口座履歴に依存していた。
- ストップ調整は生のティックの代わりに完成したローソク足によって駆動され、リポジトリガイドラインに一致する。
- インジケーター値は`Bind`を通じて処理され、直接コレクションや手動履歴バッファを避ける。

## 使用上の注意
- 最適化実行時は「手動コントロール」グループの`BuyRequest`、`SellRequest`、`CloseRequest`を無効にしておく。
- エクイティトレーリングロックと金額テイクプロフィットロジックは、利益を通貨に変換するために`Security.StepPrice`が必要。利用不可の場合、フォールバックは純粋な価格差を使用する。
- ブレークイーブンとトレーリングストップは`MinPriceStep`/`PriceStep`と小数点桁数から推測されるインストゥルメントのピップサイズを使用する。
- 要求通り、Pythonの翻訳はない。

## テスト
- 自動テストは変更されていない；戦略は既存のソリューション構造と統合され、検証のために手動パラメーター切り替えに依存する。
