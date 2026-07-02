# Wajdyss MA エキスパート戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Wajdyss MA Expert Strategy** は、MetaTrader 4 Expert Advisor「wajdyss MA Expert v3」の C# ポートです。独立した期間、計算モード、シフト、適用価格で構成された 2 つの移動平均を比較します。低速平均を上回る高速平均の強気クロスオーバーはロングエクスポージャーを開始し、弱気クロスオーバーはショートエクスポージャーを開始します。この変換では、元の資金管理ルール、反対取引のオプションの自動クローズ、および一日の終わり/週の終わりの清算フィルターが再現されます。

## 取引ロジック
1. 選択した `CandleType` (デフォルトでは 15 分足のローソク足) を購読し、各レッグに対して選択した `MovingAverageMethod` と `PriceSource` の設定を使用して高速移動平均と低速移動平均を計算します。
2. 完成したキャンドルのインジケーター値を保存します。 2 つ前は足を下回っていたものの、最後の閉じたバーで高速平均 (構成されたシフトを含む) が低速平均を上回っている場合に、強気シグナルを評価します。逆条件を使用して弱気シグナルを評価します。
3. 同じ方向の新しいエントリの間にクールダウンを強制します。この戦略は、MT4 バージョンのグローバル変数タイミング ガードを反映して、その側の最後の取引後、登録されたタイムフレームの少なくとも 1 つの完全なローソク足を待つ必要があります。
4. **AutoCloseOpposite** が有効な場合、約定待ち注文をキャンセルし、単一の成行注文で逆エクスポージャーを実行します。新しい注文量には反対方向の未処理のポジションが含まれるため、口座はすぐに反転します。
5. 毎日と金曜日の締め切りフィルターを適用します。 `DailyCloseHour`/`DailyCloseMinute` または `FridayCloseHour`/`FridayCloseMinute` を設定した後は、すべてのポジションがフラット化され、次のセッションまで新しい取引がブロックされます。

## リスクと資金の管理
- **TakeProfitPips**、**StopLossPips**、**TrailingStopPips** はピップ全体で解釈されます。実装では、セキュリティ メタデータを使用してそれらを価格ステップに変換し、元のトレーリング ロジックと同等の市場出口で StockSharp の `StartProtection` エンジンを駆動します。
- **UseMoneyManagement** は MT4 ロット計算をエミュレートします: `volume = (account_balance / BalanceReference) * InitialVolume`。交換制限は、ボリューム ステップ、最小値、および最大値のチェックによって尊重されます。
- 資金管理が無効になっている場合、注文は **InitialVolume** を直接使用します。

## パラメーター
| パラメータ | 種類 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `FastPeriod` | `int` | `10` | 高速移動平均の期間。 |
| `FastShift` | `int` | `0` | クロスオーバー値を比較する前に高速平均をシフトするバー。 |
| `FastMethod` | `MovingAverageMethod` | `Ema` | 高速ラインの移動平均モード (`Sma`、`Ema`、`Smma`、`Lwma`)。 |
| `FastPriceType` | `PriceSource` | `Close` | ローソク足価格は高速移動平均 (`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`) に入力されます。 |
| `SlowPeriod` | `int` | `20` | ゆっくりとした移動平均の期間。 |
| `SlowShift` | `int` | `0` | 比較の前に低速平均をシフトするバー。 |
| `SlowMethod` | `MovingAverageMethod` | `Ema` | 遅い回線の移動平均モード。 |
| `SlowPriceType` | `PriceSource` | `Close` | ローソク足の価格は遅い平均値に反映されます。 |
| `TakeProfitPips` | `decimal` | `100` | 利益目標までの距離 (ピップ単位) (無効にするには `0` に設定します)。 |
| `StopLossPips` | `decimal` | `50` | ピップ単位での保護停止までの距離 (無効にするには `0` に設定します)。 |
| `TrailingStopPips` | `decimal` | `0` | トレーリングストップの距離 (ピップ単位) (無効にするには `0` に設定します)。 |
| `AutoCloseOpposite` | `bool` | `true` | 反対方向に新しい取引を開始する前に、反対側のエクスポージャーを閉じてください。 |
| `InitialVolume` | `decimal` | `0.1` | 資金管理を適用する前の基本取引量。 |
| `UseMoneyManagement` | `bool` | `true` | バランスベースのポジションサイジングを有効にします。 |
| `BalanceReference` | `decimal` | `1000` | アカウント残高に応じてボリュームをスケーリングするときに使用される除数。 |
| `DailyCloseHour` | `int` | `23` | 毎日のポジションがクローズされるまでの時間 (0 ～ 23)。 |
| `DailyCloseMinute` | `int` | `45` | 日次クローズフィルターの分コンポーネント。 |
| `FridayCloseHour` | `int` | `22` | 金曜日の取引が停止するまでの時間 (0 ～ 23)。 |
| `FridayCloseMinute` | `int` | `45` | 金曜終値フィルターの分コンポーネント。 |
| `CandleType` | `DataType` | `15m` 時間枠 | 計算とクールダウンのタイミングに使用されるキャンドル シリーズ。 |

## 注意事項
- この戦略は、高レベルの StockSharp API のみに依存します。ローソク足は `SubscribeCandles` を通じて処理され、インジケーター バインディングは移動平均をフィードし、`StartProtection` はストップ注文/利益確定注文/トレーリング注文を管理します。
- ポジションのフラット化では、成行注文を使用して、MT4 エキスパートによる反対チケットの即時決済を反映します。
- このフォルダーには Python 翻訳は含まれません。 C# 実装のみが提供されます。
