# BrainTrend2 + AbsolutelyNoLagLWMA MMRec 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MetaTraderエキスパート `Exp_BrainTrend2_AbsolutelyNoLagLwma_MMRec` を2つの独立したシグナルブロックを組み合わせて再現します：トレンドフォローエンジンBrainTrend2と適応フィルターAbsolutelyNoLagLWMAです。各ブロックは独自の権限に従ってトレードを開くことも閉じることもでき、元のMMRecテンプレートのマネーマネジメントスイッチを模倣しています。注文はStockSharpの高レベルAPIを使用して成行執行と設定可能なデフォルトボリュームで実行されます。

## 取引ロジック
### BrainTrend2ブロック
* ATRに似た加重トゥルーレンジに基づいた動的トレーリングレベルを構築します。
* ローソク足がトレーリングバッファーを`0.7 * ATR`以上突き破ると方向（`river`）が切り替わります。
* 上昇リバー内の上昇ローソク足はロングエントリーをトリガーし（有効な場合）、ショートポジションを閉じます。
* 下落リバー内の下落ローソク足はショートエントリーをトリガーし（有効な場合）、ロングポジションを閉じます。
* シグナルは`Brain Signal Shift`パラメーターによって古いバーで動作するように遅延させることができます。

### AbsolutelyNoLagLWMAブロック
* 選択した価格ソースに2段階の線形加重移動平均を適用します。
* 2重LWMAが上昇すると色が**上昇（2）**に、下落すると**下落（0）**に、それ以外は**中立（1）**になります。
* 色2への遷移はロングを開き、オプションでショートを閉じます；色0への切り替えはショートを開き、オプションでロングを閉じます。
* シグナルもユーザー定義の本数分のバーで後ろにシフトできます。

### ポジション管理
* 戦略は単一のネットポジションを運用します。両方のブロックが同じバーでトレードを要求すると、クローズシグナルが新しいエントリーより前に実行されます。
* ブロックがトレードを開きたいが反対のポジションが開いており対応するクローズ権限が無効な場合、エントリーはスキップされます（単一ネットポートフォリオではヘッジポジションを保持できないことを反映しています）。

## パラメーター
| グループ | 名前 | 説明 |
| --- | --- | --- |
| BrainTrend2 | Brain Candle | BrainTrend2インジケーターに使用するローソク足タイプ。 |
| BrainTrend2 | Brain ATR | BrainTrend2の内部計算に使用するATR期間。 |
| BrainTrend2 | Brain Signal Shift | BrainTrend2シグナルを遅延させるバー数。 |
| BrainTrend2 | Brain Buy / Sell | BrainTrend2がロング/ショートトレードを開くことを許可する。 |
| BrainTrend2 | Brain Close Buys / Close Sells | BrainTrend2シグナルが既存ポジションを閉じることを許可する。 |
| AbsolutelyNoLag | Abs Candle | LWMAインジケーターに使用するローソク足タイプ。 |
| AbsolutelyNoLag | Abs Length | LWMAの期間。 |
| AbsolutelyNoLag | Abs Price | LWMAに使用する適用価格。MQLの`Applied_price_`enumに対応します。 |
| AbsolutelyNoLag | Abs Signal Shift | LWMAシグナルを遅延させるバー数。 |
| AbsolutelyNoLag | Abs Buy / Sell | LWMAブロックがロング/ショートトレードを開くことを許可する。 |
| AbsolutelyNoLag | Abs Close Buys / Close Sells | LWMAブロックがポジションを閉じることを許可する。 |
| AbsolutelyNoLag | Abs Shift | LWMA出力に定数価格オフセットを追加する。 |
| General | Order Volume | デフォルトの成行注文ボリューム。 |

## 注意事項
* ATRとLWMAの計算はMQLの元の実装に従っており、三角ATR重み付けと広範な適用価格リストが含まれています。
* スプレッド情報はStockSharpのローソク足では利用できないため、トゥルーレンジはローソク足価格のみを使用します。これはスプレッドがゼロの場合のインジケーターの動作を反映しています。
* 異なるマジックナンバーを持つ複数の同時ポジションは単一のネットポジションに統合されます。これはStockSharp戦略の標準です。
