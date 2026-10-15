# ColorMaRsi Trigger MMRec Duplex戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、MetaTraderのエキスパート **Exp_ColorMaRsi-Trigger_MMRec_Duplex.mq5** のStockSharp高レベルAPIポートです。2つの独立したMaRsi-Triggerブロックを実行します：1つはロング機会のため、もう1つはショート機会のためです。各ブロックは速いと遅い移動平均を速いと遅いRSIと比較することで生成された複合シグナルを評価します。複合値は `[-1, 1]` の範囲に制限され、元のインジケーターの動作を再現します：`+1` は強気の整合、`-1` は弱気の整合、`0` は混合状態を示します。

「MMRec」資金管理モジュールは各方向の最新トレードを監視します。設定可能な数の損失がスライディングウィンドウ内に現れると、次のトレードはパフォーマンスが回復するまで減少したボリュームに切り替わります。これはエキスパートが使用するMetaTraderライブラリ `TradeAlgorithms.mqh` の適応的なポジションサイジングロジックを再現します。

## トレードロジック

1. **インジケーターパイプライン**（ブロックごと）：
   - 選択した適用価格とタイムフレームで速いMA（`MA_fast`）と遅いMA（`MA_slow`）を計算します。
   - 可能であれば異なる適用価格で速いRSI（`RSI_fast`）と遅いRSI（`RSI_slow`）を計算します。
   - カラースコアを構築：`0` から開始し、`MA_fast > MA_slow` なら `+1` を追加、それ以外は `-1`、次に `RSI_fast > RSI_slow` なら `+1` を追加、それ以外は `-1`。結果を `[-1, 1]` にクランプします。
   - スコアの履歴を保存し、設定された `SignalBar` シフトで読み取ります（デフォルトはMetaTrader実装に一致）。

2. **ロングブロック**：
   - **エントリー**：ロングポジションが開かれていないとき（ショートは最初にカバーされます）に許可されます。前の色（`SignalBar + 1`）は `+1` でなければなりませんが、現在の色（`SignalBar`）は `≤ 0` で、強気ブロックが中立化されたことを示します。
   - **エグジット**：前の色が負（`-1`）になり、エグジットが有効な場合。

3. **ショートブロック**：
   - **エントリー**：ショートポジションが開かれていないとき（ロングが最初に決済されます）に許可されます。前の色は `-1` でなければなりませんが、現在の色は `≥ 0` で、弱気から中立への移行を示します。
   - **エグジット**：前の色が正になり、エグジットが有効な場合。

4. **ストップと目標**：オプションのストップロスとテイクプロフィット距離は価格ステップで表現され、各完成したローソク足で再評価されます。いずれかの境界を超えると、対応するポジションが即座に決済されます。

5. **資金管理**：戦略は各完了したトレードの結果（方向ごと）を保存し、最新の `HistoryDepth` トレードでの損失数をカウントします。損失数が `LossTrigger` に達すると、次の注文は減少したボリュームを使用します。そうでなければ、通常のボリュームが使用されます。

## パラメーター

| グループ | 名前 | 説明 | デフォルト |
| --- | --- | --- | --- |
| ロングブロック | `LongCandleType` | ロングMaRsi-Triggerブロックを供給するタイムフレーム。 | `H4` |
|  | `LongAllowOpen` / `LongAllowClose` | ロングポジションの開設 / 決済を有効化。 | `true` |
|  | `LongStopLossPoints` / `LongTakeProfitPoints` | インストゥルメントポイント単位の保護距離。無効にするには `0` に設定。 | `1000` / `2000` |
|  | `LongSignalBar` | インジケーターバッファをサンプリングするときにスキップする完成したバーの数。 | `1` |
|  | `LongRsiPeriod` / `LongRsiLongPeriod` | 速いと遅いRSIの長さ。 | `3` / `13` |
|  | `LongMaPeriod` / `LongMaLongPeriod` | 速いと遅い移動平均の長さ。 | `5` / `10` |
|  | `LongRsiPrice` / `LongRsiLongPrice` | 速い/遅いRSIの適用価格（Close, Open, High, Low, Median, Typical, Weighted）。 | `Weighted` / `Median` |
|  | `LongMaPrice` / `LongMaLongPrice` | 速い/遅いMAの適用価格。 | `Close` / `Close` |
|  | `LongMaType` / `LongMaLongType` | 移動平均アルゴリズム（Simple, Exponential, Smoothed, Weighted）。 | `Exponential` / `Exponential` |
| 資金管理 | `LongNormalVolume` / `LongReducedVolume` | 標準および削減されたロングトレードボリューム。 | `0.1` / `0.01` |
|  | `LongHistoryDepth` | 資金管理フィルターが観察する最近のロングトレード数。 | `5` |
|  | `LongLossTrigger` | 削減されたロングボリュームに切り替えるためのウィンドウ内の最小損失数。 | `3` |

| グループ | 名前 | 説明 | デフォルト |
| --- | --- | --- | --- |
| ショートブロック | `ShortCandleType` | ショートMaRsi-Triggerブロックを供給するタイムフレーム。 | `H4` |
|  | `ShortAllowOpen` / `ShortAllowClose` | ショートポジションの開設 / 決済を有効化。 | `true` |
|  | `ShortStopLossPoints` / `ShortTakeProfitPoints` | インストゥルメントポイント単位の保護距離。無効にするには `0` に設定。 | `1000` / `2000` |
|  | `ShortSignalBar` | インジケーターバッファをサンプリングするときにスキップする完成したバーの数。 | `1` |
|  | `ShortRsiPeriod` / `ShortRsiLongPeriod` | 速いと遅いRSIの長さ。 | `3` / `13` |
|  | `ShortMaPeriod` / `ShortMaLongPeriod` | 速いと遅い移動平均の長さ。 | `5` / `10` |
|  | `ShortRsiPrice` / `ShortRsiLongPrice` | 速い/遅いRSIの適用価格。 | `Weighted` / `Median` |
|  | `ShortMaPrice` / `ShortMaLongPrice` | 速い/遅いMAの適用価格。 | `Close` / `Close` |
|  | `ShortMaType` / `ShortMaLongType` | 移動平均アルゴリズム（Simple, Exponential, Smoothed, Weighted）。 | `Exponential` / `Exponential` |
| 資金管理 | `ShortNormalVolume` / `ShortReducedVolume` | 標準および削減されたショートトレードボリューム。 | `0.1` / `0.01` |
|  | `ShortHistoryDepth` | 資金管理フィルターが観察する最近のショートトレード数。 | `5` |
|  | `ShortLossTrigger` | 削減されたショートボリュームに切り替えるためのウィンドウ内の最小損失数。 | `3` |

## 注意事項

- 適用価格オプションはMetaTraderのセマンティクスに従います。例えば、`Weighted` は `(High + Low + 2 * Close) / 4` に相当し、`Typical` は `(High + Low + Close) / 3` に相当します。
- ロングとショートブロックが同じタイムフレームを共有する場合（デフォルト）、単一のローソク足サブスクリプションが両方の計算機を供給します。
- 損失トリガーを `0` に設定すると即座に削減されたボリュームが強制され、元の資金管理ヘルパーの動作を反映します。
- 戦略は成行注文を使用します；MetaTraderの `Deviation` パラメーターは従って必要ありません。
