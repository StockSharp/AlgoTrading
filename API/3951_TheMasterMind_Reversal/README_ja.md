# MasterMind 逆転戦略 (StockSharp ポート)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 4 エキスパート アドバイザ「TheMasterMind」のポート。Stochastic オシレーターと Williams %R を組み合わせて、極端な反転を捉えます。
- ローソク足サブスクリプションとインジケーター バインディングを使用して、StockSharp の高レベルの API で実装されます。
- 単一の証券を取引し、終了したローソク足のみに反応し、元の「クローズでの取引」実行スタイルを反映します。

## 取引ロジック
1. **インジケーターの準備**
   - `StochasticOscillator` は、構成可能な %K/%D スムージングと合計ルックバック長を備えた %D 信号ラインを提供します。
   - `WilliamsR` は、最近の高値/安値範囲内の終値の相対位置を測定します。
2. **エントリールール**
   - `%D <= 3` _and_ `Williams %R <= -99.5` のときに **買い**、これは下限を下回る WPR の深い浸透とともに、確率論的な売られ過ぎの極値を示しています。
   - `%D >= 97` _and_ `Williams %R >= -0.5` のときに **売り**、Williams %R が 0 付近に留まることで確認された買われすぎの極度のシグナルです。
   - 反対のポジションが存在する場合は、最初にフラット化され、その後、設定された基本ボリュームで新しい成行注文が送信されます。
3. **退出ルール**
   - リバースシグナルは、現在のポジションをクローズし、方向を反転します（一度に 1 つのポジション、MQL スクリプトで使用されるヘッジ無効モードに一致します）。
   - オプションの `StartProtection` ストップロス、テイクプロフィット、およびトレーリングストップサービスは、戦略開始ごとに 1 回だけ保護エグジットを処理します。

## リスク管理
- パラメータ `StopLoss`、`TakeProfit`、`UseTrailingStop`、`TrailingStop`、および `TrailingStep` は、元の EA の資金管理制御にマッピングされます。
- ブローカーに依存しないように、すべての距離は絶対価格単位で表されます。それぞれの保護機能を無効にするには、これらを `0` のままにしておきます。
- `StartProtection` は、少なくとも 1 つの保護距離がゼロ以外の場合に自動的にアクティブになります。

## Strategy Parameters
| パラメータ | 説明 | デフォルト |
|-----------|-------------|---------|
| `TradeVolume` | 新規エントリーごとの基本ロットサイズ。 | `1` |
| `StochasticPeriod` | 確率的オシレーターの合計ルックバック。 | `100` |
| `KPeriod` | %K スムージング長。 | `3` |
| `DPeriod` | %D 信号の長さ。 | `3` |
| `WilliamsPeriod` | Williams %R のルックバックの長さ。 | `100` |
| `StochasticBuyThreshold` | Long を許可するには、%D がこの上限を下回っていなければなりません。 | `3` |
| `StochasticSellThreshold` | ショートを許可するには、%D がこの値を超えていなければならない下限。 | `97` |
| `WilliamsBuyLevel` | Williams %R の売られ過ぎレベル。 | `-99.5` |
| `WilliamsSellLevel` | Williams %R の買われすぎレベル。 | `-0.5` |
| `StopLoss` | 絶対的なストップロス距離。 | `0` |
| `TakeProfit` | 絶対的なテイクプロフィットディスタンス。 | `0` |
| `UseTrailingStop` | `true` の場合、トレーリング保護を有効にします。 | `false` |
| `TrailingStop` | 絶対的なトレーリングストップ距離。 | `0` |
| `TrailingStep` | トレーリング中にステップが適用されました。 | `0` |
| `CandleType` | プライマリキャンドルサブスクリプションの時間枠 (デフォルトは 15 分)。 | `15m time frame` |

## 実装メモ
- この戦略は、`SubscribeCandles(CandleType)` を介して単一のローソク足シリーズをサブスクライブし、`BindEx` を使用して確率指標と Williams %R インジケーターをバインドします。
- 取引の決定は、`candle.State == CandleStates.Finished` と `IsFormedAndOnlineAndAllowTrading()` が満たされた場合にのみ行われます。
- チャート ヘルパー (`DrawCandles`、`DrawIndicator`、`DrawOwnTrades`) は、インジケーターと取引を視覚化するためにチャート領域が使用できるときに呼び出されます。
- ログ ステートメント (`LogInfo`) は元のアラート文字列を反映しており、ライブ取引やバックテスト中の意思決定プロセスを追跡するのに役立ちます。
