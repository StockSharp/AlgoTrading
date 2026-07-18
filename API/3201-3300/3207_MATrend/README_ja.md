# 3207 – MA Trend戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**MA Trend戦略**は、StockSharpの高レベルAPIを使用してMetaTraderエキスパート *MA Trend.mq5* を再現します。ボットは設定可能な前方シフトを持つ単一の線形加重移動平均に従います。終値がシフトした平均を上回ると戦略はロングになり、平均を下回るとショートポジションを開きます。オプションのストップロス、テイクプロフィット、トレーリングストップのルールは元のMQL実装のリスク制御を反映します。

## トレードロジック
1. 設定されたロウソク足タイプ（デフォルト：1分時間軸）を購読し、選択した方法と価格ソースを使用して移動平均を計算します。
2. 最新の終値と比較する前に、要求された数の確定ロウソク足だけ移動平均値を前方にシフトします。
3. シグナルを生成する：
   - **ロング** – 終値がシフトしたMAを上回る（`ReverseSignals` が有効な場合は反転）。
   - **ショート** – 終値がシフトしたMAを下回る（`ReverseSignals` が有効な場合は反転）。
4. ポジション管理オプションを適用する：
   - `CloseOpposite` が `true` の場合、取引を開く前に反対のエクスポージャーを閉じる。
   - `OnlyOnePosition` が有効でポジションが既に存在する場合、新しいエントリーをブロックする。
5. pipで表現されたストップロス、テイクプロフィット、トレーリングストップ距離で出口を管理する。トレーリングロジックはMQLエキスパートと同様に、ストップを引き締める前に価格が `TrailingStopPips + TrailingStepPips` だけ動くことを要求します。

## パラメーター
| 名前 | 型 | デフォルト値 | 説明 |
|------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | ロット/契約単位の注文サイズ。 |
| `StopLossPips` | `int` | `50` | pip単位のストップロス距離。ゼロで固定ストップを無効化。 |
| `TakeProfitPips` | `int` | `140` | pip単位のテイクプロフィット距離。ゼロでターゲットを無効化。 |
| `TrailingStopPips` | `int` | `15` | トレーリングストップ距離。トレーリングを無効化するにはゼロに設定。 |
| `TrailingStepPips` | `int` | `5` | トレーリングストップを動かす前に必要な追加pip。`TrailingStopPips` がゼロより大きい場合は正のままでなければならない。 |
| `MaPeriod` | `int` | `12` | 移動平均の長さ。 |
| `MaShift` | `int` | `3` | 移動平均を前方にシフトするために使用する確定バーの数。 |
| `MaMethod` | `MovingAverageKind` | `Weighted` | 移動平均計算モード（Simple、Exponential、Smoothed、Weighted）。 |
| `AppliedPrice` | `AppliedPriceMode` | `Weighted` | インジケーター入力として使用するロウソク足価格（Close、Open、High、Low、Median、Typical、Weighted）。 |
| `OnlyOnePosition` | `bool` | `false` | 戦略を単一のオープンポジションに制限する。 |
| `ReverseSignals` | `bool` | `false` | ロング/ショートシグナルの方向を入れ替える。 |
| `CloseOpposite` | `bool` | `false` | 新しいポジションに入る前に反対のエクスポージャーを閉じる。 |
| `CandleType` | `DataType` | `1 minute` | インジケーターに提供するロウソク足タイプ/時間軸。 |

## 注意事項
- pip サイズは元のMetaTrader動作に合わせて3/5小数点の価格を持つ銘柄に自動的に適応します。
- トレーリングストップ検証はMQLチェックを再現します：`TrailingStopPips > 0` かつ `TrailingStepPips <= 0` の場合、戦略は起動時に例外をスローします。
- すべてのインジケーター更新と注文決定は確定したロウソク足のみを使用し、決定論的なバックテストを保証します。
