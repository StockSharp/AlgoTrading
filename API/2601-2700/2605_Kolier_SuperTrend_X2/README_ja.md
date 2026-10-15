# Kolier SuperTrend X2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、異なる時間軸で動作する2つのSuperTrendフィルターを組み合わせることで、オリジナルのMetaTraderエキスパートを再現します。上位時間軸のSuperTrendが市場の支配的なバイアスを定義し、下位時間軸のSuperTrendがエントリーを発動する同期ブレイクアウトを探します。StockSharpポートは高レベルAPIバインディングを使用するため、インジケーターはロウソク足の更新を直接受け取り、独自の履歴を維持します。

## トレードロジック
- **トレンドフィルター:** 上位時間軸のSuperTrendが上昇または下降トレンドを確認する必要があります。確認遅延は`TrendSignalShift`で制御され、モード（`TrendMode`）は単一バー（`NewWay`）または2本連続バー（その他全モード）のどちらが必要かを定義します。
- **エントリーシグナル:** 下位時間軸のSuperTrendは、現在のトレンドフィルターと一致した方向転換を待ちます。`EntrySignalShift`はシグナルを遅延させて完全に閉じたバーに依拠し、`EntryMode`は戦略が即座に反応するか（`NewWay`）確認済みリバーサル後にのみ反応するか（その他モード）を制御します。
- **ロングエントリー:** `EnableBuyEntries`が`true`で、トレンドフィルターが強気であり、選択したモードに従ってエントリーSuperTrendが強気に転換したときに許可されます。既存のショートポジションを先にクローズし、その後`Volume + |Position|`のボリュームでロングポジションをオープンします。
- **ショートエントリー:** `EnableSellEntries`が`true`で、トレンドフィルターが弱気であり、エントリーSuperTrendが弱気に転換したときに許可されます。ショートエントリー前に既存ロングポジションをカバーします。
- **エグジット:**
  - 上位時間軸での反転はロング（`CloseBuyOnTrendFlip`）またはショート（`CloseSellOnTrendFlip`）をクローズします。
  - エントリー時間軸の転換も、`CloseBuyOnEntryFlip`/`CloseSellOnEntryFlip`が有効な場合にポジションをクローズできます。
  - オプションの固定ストップ（`StopLossPoints`、`TakeProfitPoints`）は`Security.PriceStep`の倍数として適用されます。

## インジケーター
- StockSharp `SuperTrend` の2インスタンス（1つはトレンド時間軸用、1つはエントリー用）。

## パラメーター
- `TrendCandleType` – トレンドフィルターの時間軸。
- `EntryCandleType` – エントリーシグナルの時間軸。
- `TrendAtrPeriod`、`TrendAtrMultiplier` – トレンドSuperTrendのATR設定。
- `EntryAtrPeriod`、`EntryAtrMultiplier` – エントリーSuperTrendのATR設定。
- `TrendMode`、`EntryMode` – 確認モード：`NewWay`は1バー後に反応し、その他モードは2本連続バーが必要（このポートではVisualとExpertSignalはクラシックSuperTrendと同様に動作）。
- `TrendSignalShift`、`EntrySignalShift` – インジケーター値を使用する前に待機する閉じたバーの数。
- `EnableBuyEntries`、`EnableSellEntries` – ロング/ショートトレードを有効化。
- `CloseBuyOnTrendFlip`、`CloseSellOnTrendFlip` – トレンドフィルターからの逆シグナルでエグジット。
- `CloseBuyOnEntryFlip`、`CloseSellOnEntryFlip` – エントリー時間軸からの逆シグナルでエグジット。
- `StopLossPoints`、`TakeProfitPoints` – 保護注文の価格ステップ単位の距離（0で無効化）。
- `Volume` – 新規ポジションのベースボリューム。
- `Slippage` – ソースエキスパートとの互換性のために保持されたプレースホルダーパラメーター。

## 注記
- このポートはStockSharpの高レベルワークフローに焦点を当てています：ロウソク足は`SubscribeCandles`で購読され、インジケーターは`BindEx`でバインドされ、戦略は最小限の状態（トレンド方向、ストップレベル）のみを保持します。
- `StartProtection()`は標準StockSharpポジション保護ヘルパーを有効化するために1回呼び出されます。
