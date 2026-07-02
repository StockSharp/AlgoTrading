# ターングリッド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

The **TurnGrid Strategy** replicates the behaviour of the original MQL5 Expert Advisor `TurnGrid.mq5`.現在の市場価格を中心に対称的な価格グリッドを構築し、価格が 1 つのグリッド セルから別のグリッド セルに移動するたびにロング注文とショート注文を交互に切り替えます。 The strategy continuously rebalances open orders to maintain both bullish and bearish exposure until the configured equity target is achieved.

変換には、StockSharp の高レベルの API が使用されます。つまり、ローソク足のサブスクリプションがグリッドの更新を駆動し、成行注文がエントリーとエグジットを処理し、リスク管理が戦略パラメーターを通じて表現されます。 All comments have been translated into English and the naming follows StockSharp conventions.

## 取引ロジック

1. When the strategy starts it captures the latest candle close and builds a grid containing `4 * GridShares` levels. The central level is set to the current price, upper levels scale by `1 + GridDistance`, and lower levels scale by `1 - GridDistance`.
2. An initial market buy order is placed at the centre of the grid.そのボリュームは、利用可能な予算部分 (`Balance / GridShares`) と、MQL バージョンから継承された増分ステーク式から計算されます。
3. Every finished candle updates the current grid index based on the close price.インデックスが変更された場合:
   - 新しいインデックスから 2 レベル離れたチケットにリンクされたポジションは閉じられます (価格より低い購入チケットは売却され、より高い価格で販売されたチケットは買い戻されます)。
   - New positions are opened to keep both long and short anchors on the active level. If neither side is present, the strategy opens the side with fewer active positions to balance exposure.
4. Fees are approximated via the `FeeRate` parameter. Each filled order contributes to a running fee total used when evaluating performance.
5. アカウントの資本（累積手数料推定値を差し引いた後）が初期残高を `EquityTakeProfit` 超えると、ストラテジーはネットポジションを閉じ、最新の価格を中心にグリッドを再構築します。

## パラメーター

| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `GridDistance` | 隣接するグリッド レベル間の相対距離。 | `0.01` |
| `GridShares` | Maximum number of concurrent grid positions that can be active. | `50` |
| `EquityTakeProfit` | Percentage gain over the initial balance required to reset the grid. | `0.02` |
| `FeeRate` | Estimated transaction fee per trade, applied to executed volume. | `0.0008` |
| `CandleType` | 戦略を推進するために使用されたキャンドル シリーズ。 | `1` 分の時間枠 |

## 実装メモ

- キャンドルのサブスクリプションは `SubscribeCandles(CandleType)` を介して処理され、戦略は終了したキャンドルにのみ反応し、StockSharp との互換性を維持しながら、元の EA のティック駆動ロジックと一致します。
- グリッドの状態は、価格アンカー、ブール値フラグ、遅延クロージャのチケット ボリュームを含む `GridLevel` 構造体の軽量配列に保存されます。
- 注文サイズは元の増分資本配分式に従い、証券の `VolumeStep`、`VolumeMin`、および `VolumeMax` 設定による追加の正規化が行われます。
- Equity-based resets wait for the current net position to close before rebuilding the grid, ensuring clean transitions between trading cycles.

## ファイル

- `CS/TurnGridStrategy.cs` – C# implementation of the strategy using StockSharp high-level constructs.
- `README.md` – 英語のドキュメント (このファイル)。
- `README_zh.md` – 簡体字中国語のドキュメント。
- `README_ru.md` – ロシア語のドキュメント。
