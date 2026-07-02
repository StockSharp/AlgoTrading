# News Release戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、予定されたニュース発表の周囲に待機注文のブラケットを準備し、結果として生じるポジションを能動的に管理することで、元の**NewsReleaseEA**エキスパートアドバイザーの中核動作を再現します。

## 主要アイデア

- 5つの入力（ニュース時刻、前後ウィンドウ、注文距離、間隔）が、stop注文をいつどこへ置くかを定義します。
- 設定されたニュース時刻の直前に、buy stopとsell stopの対称セットを送信します。最初のペアは現在のask/bidから`DistancePips`離して置き、追加ペアは`StepPips`ずつずらします。
- 待機注文はイベント後`PostNewsMinutes`分まで有効です。ウィンドウ終了時、戦略はすべてのアクティブ注文をキャンセルし、要求されていればオープンポジションを閉じます。
- 注文が約定すると、反対側の待機注文は自動キャンセルされ、オープンポジションはpipsで表すストップロス、テイクプロフィット、ブレイクイーブン、トレーリングルールで管理されます。
- ブレイクイーブン保護は、価格がポジションに有利に`BreakEvenTriggerPips`動いた後に起動し、価格がエントリー価格プラス`BreakEvenOffsetPips`（ロング）またはマイナスそのオフセット（ショート）へ戻ると決済を強制します。
- トレーリング管理はエントリー後に到達した最良価格を追跡します。現在価格と極値の距離が`TrailingPips`を超えると、蓄積利益を守るためにポジションを閉じます。
- `TradeOnce`フラグは、最初の取引完了後に二度目の起動を防ぎ、MQLプログラムの「ニュースごとに1回だけ取引」動作を反映します。

## パラメーター

- `NewsTime`: ニュース発表予定時刻。
- `PreNewsMinutes`: 発表の何分前に待機注文を置くか。
- `PostNewsMinutes`: 発表後、待機注文をキャンセルするまで何分維持するか。
- `OrderPairs`: ブラケットを構成するbuy stop/sell stopペア数。
- `DistancePips`: 配置時点の現在最良ask/bidから最初のペアまでの距離（pips）。
- `StepPips`: 連続するペア間の追加間隔（pips）。
- `OrderVolume`: 各待機注文で送信する数量。
- `TradeOnce`: 有効な場合、戦略はイベントウィンドウごとに一度だけ取引できます。
- `UseStopLoss` / `StopLossPips`: ストップロス距離（pips）を有効化し設定します。
- `UseTakeProfit` / `TakeProfitPips`: テイクプロフィット距離（pips）を有効化し設定します。
- `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips`: ブレイクイーブンモジュールを設定します。
- `UseTrailing` / `TrailingPips`: トレーリング決済ロジックを有効化し、距離をpipsで定義します。
- `CloseAfterEvent`: ニュース後ウィンドウ終了時にオープンポジションを閉じます。

## 注記

- 戦略はlevel1データ（`SubscribeLevel1`）だけで動作し、ローソク足を待たずに最新bid/ask価格へ反応できます。
- pipsで表す価格距離は、銘柄の`PriceStep`を使って絶対価格へ変換されます。`PriceStep`がない場合は安全なフォールバックとして1を使います。
- ストップロス、テイクプロフィット、ブレイクイーブン、トレーリング条件は、`ClosePosition()`を呼び出して成行でポジションを閉じます。これは元エキスパートの反応的管理を反映し、実装をコンパクトに保ちます。
- 要望通り、Python版は提供されていません。
