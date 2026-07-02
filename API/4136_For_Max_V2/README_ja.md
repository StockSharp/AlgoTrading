# Max V2の場合
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
For Max V2 is a port of the MetaTrader 4 expert advisor `for_max_v2.mq4`.この戦略は、特定の 2 本のローソク足の巻き込みパターンを待ってから、最新のローソク足の周囲に対称的な買いストップ注文と売りストップ注文のペアを配置します。ブレイクアウト注文が約定すると、反対側の未決注文が削除され、ポジションは固定ストップ、オプションのテイクプロフィットレベル、損益分岐点で最初に少額の利益を確定させてから価格に従うトレーリングルーチンで管理されます。

## 戦略ロジック
### 巻き込みパターンの検出
The original expert advisor exposes two entry blocks and both are preserved:
* **タイプ 1 セットアップ** – 以前の `Max Search` ローソク足をスキャンし (現在のバーをスキップ)、その範囲内の最低安値が 2 バー前に発生するのを待ちます。**または**最高高値が 2 バー前に発生するのを待ちます。 When that happens the candle two bars back must engulf the previous candle (higher high and lower low).セットアップは、最後に完成したキャンドルの周りにまたがってアームします。
* **Type 2 setup** – also scans the previous `Max Search` candles, but looks for the extreme to appear one bar ago. In addition, the candle one bar ago must engulf the candle two bars back. A straddle is then placed around the most recent candle.両方のセットアップは共存できます。 each manages its own pending orders and expiration clock.

### 保留中の注文
* **エントリー価格** – 買いストップ注文は前のローソク足の高値プラス `Gap Points` で発注され、売りストップ注文は前のローソク足の安値から `Gap Points` を引いた位置で発注されます。
* **ストップロス** – タイプ 1 の場合、ロングストップは 2 バー前のローソク足の安値 (マイナスギャップ) に固定され、ショートストップはそのローソク足の高値 (プラスギャップ) に固定されます。タイプ 2 は、両側に前のキャンドルを使用します。
* **利益確定** – オプション。ロングターゲットは前の高値から `Gap Points + Buy Take Profit Points` を加算し、ショートターゲットは前の安値から `Gap Points + Sell Take Profit Points` を減算します。 Setting the take-profit inputs to `0` disables the respective targets.
* **Expiration** – each straddle carries a validity timestamp computed as `Order Expiry (bars)` multiplied by the configured candle timeframe. If the pending orders are still working when the timestamp is reached, both sides are cancelled.

### ポジション管理
* Once a buy-stop fills, any remaining sell-stop orders from either setup are cancelled;対称ルールは短いエントリの後に適用されます。
* ストップとターゲットは完成したキャンドルで監視されます。 If a candle’s low reaches the long stop (or the high reaches the short stop) the position is closed with a market order. The same approach is used for the take-profit levels.
* 損益分岐点ルーチン (`Break-even Trigger` および `Break-even Offset`) は、ポジションがトリガー量だけ進むと、エントリー価格に構成されたオフセットをプラス/マイナスした値にストップを移動します。
* 後続ブロックは、ストップを最良のエクスカーションから `Long/Short Trailing Buffer` ポイント遠ざけますが、それは価格が十分に上昇した後のみです (オプションで取引がすでに利益を上げている後のみ)。 `Trailing Step` は、ストップを再度締める前に最小限の改善を要求することで、過度の調整を防ぎます。

## パラメーター
* **出来高** – 未決の各ストップ注文の注文量。
* **バイプロフィット (ポイント)** – ロングテイクプロフィットの計算に使用されるポイント単位の距離 (無効にする場合は `0` に設定します)。
* **売りテイクプロフィット (ポイント)** – ショートテイクプロフィットの計算に使用されるポイント単位の距離 (無効にするには `0` に設定します)。
* **ギャップ (ポイント)** – ストップエントリーを配置する前に高値/安値にバッファーが追加され、テイクプロフィットディスタンスに組み込まれます。
* **Search Depth** – number of finished candles scanned when checking for Type 1 and Type 2 engulfing setups.
* **注文有効期限 (バー)** – 両側がキャンセルされるまで保留中のストラドルがアクティブのままになるローソク足の長さの数。
* **損益分岐点トリガー (ポイント)** – 損益分岐点調整を開始する利益のしきい値。
* **損益分岐点オフセット (ポイント)** – 損益分岐点ストップが設定されたときにエントリー価格に追加される追加のバッファー。
* **ロングトレーリングバッファー (ポイント)** – 損益分岐点に達した後のロングポジションのトレーリング距離。
* **ショート トレーリング バッファー (ポイント)** – 損益分岐点に達した後のショート ポジションのトレーリング距離。
* **トレーリング ステップ (ポイント)** – トレーリング ストップを再度更新する前に必要なストップ位置の最小限の改善。
* **利益後のトレーリングのみ** – 有効にすると、トレーリングはポジションがバッファーを越えるまで待機してからアクティブになります。
* **ローソク足タイプ** – パターン検出、注文の有効期限、および終了処理に使用されるローソク足の時間枠。

## 追加の注意事項
* 「ポイント」で表される価格オフセットは、証券の `PriceStep` に依存します。小数点以下 5 桁（または 3 桁）のシンボルは、MetaTrader と同様に、小数点以下のピップ サイズに自動的に変換されます。
* ストップロスとテイクプロフィットは、閉じたローソク足のレベルを管理する EA の動作を反映するために、戦略内の成行注文を通じて実行されます。
* この戦略では、元のソースの未使用の `vhod_3` 関数は実装されていません。 2 つのアクティブなエントリ ブロックのみが移植されました。
* このパッケージには C# 実装のみが含まれています。 Python のバージョンは提供されていません。
