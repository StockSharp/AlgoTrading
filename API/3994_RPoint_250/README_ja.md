# RPoint 250 リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**RPoint 250 リバーサル戦略**は、MetaTrader 4 エキスパート アドバイザー `e_RPoint_250` の StockSharp 移植です。オリジナルロボット
は、最新のスイング高とスイング低を強調表示する *RPoint* と呼ばれるカスタム インジケーターに依存しています。なぜならその指標は
StockSharp では利用できませんが、変換では組み込みの `Highest` および `Lowest` インジケーターを使用して同じ動作が再現されます。
新しい極値が以前に検出された極値に置き換わるたびに、戦略は即座に位置を反転し、同じ位置を復元します。
MQL バージョンで定義されたストップロス、テイクプロフィット、トレーリングロジック。

## 取引ワークフロー

1. `CandleType` で指定されたローソク足シリーズを購読します (デフォルト: 5 分足ローソク足)。
2. Track the rolling maximum and minimum over the last `ReversePoint` bars.これらの値は、エミュレートされた RPoint レベルを表します。
3. 価格が新たな最高値を更新した場合は、ロングポジションをクローズし、ボリューム`OrderVolume`のショートポジションをオープンします。
4. 価格が新たな安値を記録した場合は、ショート ポジションをクローズし、ボリューム `OrderVolume` のロング ポジションをオープンします。
5. `StartProtection` を使用して保護命令を適用します。ストップロスとテイクプロフィットの距離は、以下の価格ポイントで表されます。
パラメータ `StopLossPoints` と `TakeProfitPoints`。
6. 必要に応じて、`TrailingStopPoints` まで利益を追跡します。 The trailing engine measures how far price has moved in favour of the
ポジションを設定し、価格が設定されたポイント数だけリトレースしたときにクローズします。
7. 同じバー内で複数の取引を開始することを避けるために、最後に成功したエントリーのローソク足の時間を覚えておいてください。
`TimeN` は、MQL スクリプトから保護します。

この戦略では常に最大 1 つのオープン ポジションが維持されます。 It closes existing trades before entering in the opposite direction and
スケールインすることはありません。

## パラメーター

| パラメータ | 種類 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | 各成行注文で送信されるボリューム。 `Lots` 入力を MetaTrader バージョンにミラーリングします。 |
| `TakeProfitPoints` | `decimal` | `15` | Distance to the take-profit order measured in price points.利益目標を無効にするには、`0` に設定します。 |
| `StopLossPoints` | `decimal` | `999` | Distance to the protective stop expressed in price points.固定ストップなしで取引するには、`0` に設定します。 |
| `TrailingStopPoints` | `decimal` | `0` | 価格帯のオプションのトレーリング距離。ゼロの場合、後続ロジックは無効になります。 |
| `ReversePoint` | `int` | `250` | 最新のスイングハイとスイングローを検索する際に考慮されるローソク足の数。値を大きくするとノイズが滑らかになります。 |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | 戦略によって分析されたローソク足の集計。 Change it to match the chart timeframe used in MetaTrader. |

## 実装メモ

- `Highest` と `Lowest` は、高レベルの `Bind` API を介してキャンドル サブスクリプションにバインドされているため、手動インジケーター キューはありません。
必須です。
- `StartProtection` reproduces the original stop-loss and take-profit distances in absolute price units. StockSharp が処理します
新しいポジションが現れたら注文を出します。
- トレーリングストップは、完了した各ローソク足を監視することによって実装されます。 When price retreats by the configured number of points from
エントリー後に最高価格が達成された場合、ポジションは成行注文でクローズされます。
- このクラスは、重複を避けるために、最後に実行された反転レベル (`_executedHighLevel` および `_executedLowLevel`) を保存します。
エントリ。 This is equivalent to the `Reverse_High` / `Reverse_Low` variables in the MQL code.
- The `_lastSignalTime` field mirrors the `TimeN` variable and blocks multiple orders inside the same candle, preventing
accidental double submissions on illiquid markets.

## 使用ガイドライン

1. Attach the strategy to a portfolio that supports the selected instrument and candle type.
2. ブローカーの契約規模とリスク管理ルールに準拠するように `OrderVolume` を調整します。
3. Tune `ReversePoint` to match the volatility of the traded asset. Higher values yield fewer but more meaningful reversals.
4. `StopLossPoints`、`TakeProfitPoints`、および `TrailingStopPoints` がセキュリティの `PriceStep` と互換性があることを確認します。
5. Run a backtest in StockSharp Designer or Backtester to confirm the behaviour before trading live capital.
6. Monitor the log output: informational messages will highlight position changes and can help validate the conversion.

RPoint インジケーターは組み込みコンポーネントで近似されるため、MetaTrader の実行との小さな違いは次のとおりです。
ギャップや異なる丸めルールのある履歴データでも可能です。独自の市場データ フィードを使用して結果を常に検証します。
本番環境で戦略に頼る前に。
