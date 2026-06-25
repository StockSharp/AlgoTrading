# Two MA One RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTrader 5エキスパート「Two MA one RSI」をStockSharpに移植します。高速と低速の移動平均クロスオーバーを前の閉じたロウソク足で評価したRSI確認と組み合わせます。柔軟なスイッチにより各比較を「より大きい」または「より小さい」ルールに変換でき、コードを変更せずに設定を反転させることができます。

## 詳細
- **エントリー条件**：
  - ロングシグナルは、高速MAが2バー前に低速MAより下にあり、最新の閉じたバーで高速MAが低速MAより上にあり、前のバーのRSIが上部閾値より上であることを必要とします。各比較はブール値パラメーターで反転できます。
  - ショートシグナルはロジックを反映し、反対のMA関係とRSIが下部閾値を下回っていることを確認します。
  - 両MAは同じ平均化タイプを使用します；遅い期間は常に`FastMaPeriod * SlowPeriodMultiplier`です。オプションの水平シフトはインジケーター値を数ロウソク足前に読み取るMT5の動作を再現します。
- **ロング/ショート**：戦略は両方向にポジションを開くことができます。`CloseOppositePositions`は新しいシグナルが入場前に反対側を強制決済するかどうかを制御します。
- **エグジット条件**：
  - pipで設定可能なストップロスとテイクプロフィット。
  - オプションのトレーリングストップ。価格がエントリーを超えて少なくとも`TrailingStopPips + TrailingStepPips`進んだ後にのみ移動します。
  - `ProfitClose`は浮動P&L（インジケーターのステップ価格を使用）を監視し、目標通貨額に達すると全ポジションを決済します。
- **ストップ**：`StopLossPips`がゼロの場合、戦略はトレーリングストップモジュールのみに依存します（有効な場合）。`TrailingStopPips`は正の`TrailingStepPips`を必要とし、元のエキスパートの検証に一致します。
- **デフォルト値**：
  - `FastMaPeriod = 10`、`SlowPeriodMultiplier = 2`。
  - `FastMaShift = 3`、`SlowMaShift = 0`。
  - `RsiPeriod = 10`、`RsiUpperLevel = 70`、`RsiLowerLevel = 30`。
  - `StopLossPips = 50`、`TakeProfitPips = 150`、`TrailingStopPips = 15`、`TrailingStepPips = 5`。
  - `MaxPositions = 10`、`ProfitClose = 100`、`TradeVolume = 1`。
- **フィルター**：6つのブールスイッチ（`BuyPreviousFastBelowSlow`、`BuyCurrentFastAboveSlow`、`BuyRequiresRsiAboveUpper`、`SellPreviousFastAboveSlow`、`SellCurrentFastBelowSlow`、`SellRequiresRsiBelowLower`）によりユーザーが各比較の意味を即座に変更できます。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `CandleType` | 分析に使用する時間軸（またはその他のロウソク足タイプ）。 |
| `MaType` | 移動平均のファミリー（単純、指数、平滑化、加重、出来高加重）。 |
| `FastMaPeriod` | 高速MAの期間。 |
| `SlowPeriodMultiplier` | 低速MAの期間乗数（`低速 = 高速 * 乗数`）。 |
| `FastMaShift`、`SlowMaShift` | MA値評価時に適用するロウソク足での水平シフト。 |
| `RsiPeriod` | RSIの長さ（前の完了したロウソク足を使用）。 |
| `RsiUpperLevel`、`RsiLowerLevel` | ロングとショートの確認のためのRSI閾値。 |
| `BuyPreviousFastBelowSlow`、`BuyCurrentFastAboveSlow`、`BuyRequiresRsiAboveUpper` | ロングシグナルの比較を切り替え。 |
| `SellPreviousFastAboveSlow`、`SellCurrentFastBelowSlow`、`SellRequiresRsiBelowLower` | ショートシグナルの比較を切り替え。 |
| `StopLossPips`、`TakeProfitPips` | pipで測定した保護ストップとターゲット（pip サイズは銘柄の価格ステップから導出）。 |
| `TrailingStopPips`、`TrailingStepPips` | トレーリングストップ距離と最小改善値。 |
| `MaxPositions` | 方向ごとの同時エントリーの最大数（`0` = 無制限）。 |
| `ProfitClose` | 達成時に全ポジションを決済する通貨利益目標。 |
| `CloseOppositePositions` | 新しいトレードを開く前に反対側を決済するかどうか。 |
| `TradeVolume` | 基本注文サイズ；戦略の`Volume`プロパティとも同期します。 |

## 実装メモ
- すべての決定は完了したロウソク足のみを使用し、MT5エキスパートの「新しいバー」ロジックに一致します。
- pip サイズはインジケーターの価格ステップに等しくなります。市場が分数pip価格を使用する場合、元のエキスパートの`digits_adjust`ロジックに一致するよう銘柄設定を適宜調整してください。
- トレーリングストップは価格が`TrailingStopPips + TrailingStepPips`進んだ後にのみ開始します；ストップは終値から`TrailingStopPips`離れたところに固定され、少なくとも`TrailingStepPips`改善された場合にのみ移動します。
- `ProfitClose`は銘柄の`PriceStep`と`StepPrice`を使用して浮動利益を計算します。正確な通貨結果のためにそれらのフィールドが設定されていることを確認してください。
