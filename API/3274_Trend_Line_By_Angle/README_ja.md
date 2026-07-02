# 角度指定トレンドライン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、MetaTrader のエキスパートアドバイザー *Trend Line By Angle* を StockSharp に移植したものです。元のロボットは、手動ボタンによるエントリーと広範な資金管理ツールを組み合わせていました。この移植版は、裁量的なワークフローを自動化された MACD トレンドフォローシステムに変換しながら、保護ロジックを維持しています。

- 設定されたシグナル用ローソク足タイプで計算される月足 MACD (12/26/9) が方向を決定します。強気クロスはロングエクスポージャーを開き、弱気クロスはショートエクスポージャーを開きます。
- エントリーは設定されたブロック数まで積み増され、元の EA における手動クリックの繰り返しを再現します。
- Bollinger Bands (20, 2) が実行時間枠を監視します。上側バンドへの接触でロングエクスポージャーを清算し、下側バンドへの接触でショートを清算して、MetaTrader の視覚的なストップボタンを再現します。
- stop-loss、take-profit、trailing stop、break-even 移動というクラシックなリスク管理は、商品の `PriceStep` を通じて変換された pip 距離で動作します。
- 口座レベルの保護は、金額または割合の利益目標に達したときにすべての注文を閉じます。追加の金額ベース trailing ロックは含み益を追跡し、設定された drawdown でエグジットします。

## 実行フロー

1. **インジケーター準備** - `MovingAverageConvergenceDivergenceSignal` は `SignalCandleType` で実行され、`BollingerBands` は取引用の `CandleType` で実行されます。
2. **エントリーシグナル** - 確定した各実行ローソク足で、最新の MACD クロスを評価します。上抜けクロスは `BuyMarket` を発動し、下抜けクロスは `SellMarket` を発動します。反転する前に、既存の反対方向エクスポージャーを閉じます。
3. **スケーリングロジック** - 集計ポジションが `TradeVolume * MaxEntries` に達するまで、戦略は買い/売りを続けます。
4. **リスク管理** - break-even、trailing stop、stop-loss、take-profit の水準は各ローソク足で再計算されます。他の水準に到達していなくても、Bollinger への接触は強制的にエグジットします。
5. **口座保護** - 新しいシグナルを生成する前に、金額および割合の take-profit チェックを実行します。金額 trailing モジュールは最高の総 PnL を追跡し、下落が `MoneyTrailStop` を超えるとすべてを閉じます。

## 資金管理の詳細

- **総PnL** は、実現利益 (`PnL`) と、ローソク足終値、価格ステップ、ステップ値から計算した含み PnL の合計です。
- **Break-even** は、値動きが `BreakEvenTriggerPips` を超えると、保護ストップを `Entry + BreakEvenOffsetPips` (ロング) または `Entry - BreakEvenOffsetPips` (ショート) に移動します。
- **Trailing stop** は、利益が `TrailingStopPips` を超えるたびに価格へ近づきます。ロングの trailing 水準は上がるだけで、ショートの trailing 水準は下がるだけです。
- **金額trail** は、`MoneyTrailTrigger` の利益が確認された後に有効になります。それ以降は最高利益を記憶し、そのピークから `MoneyTrailStop` を超えて失うとすべてのポジションを閉じます。

## パラメーター

| パラメーター | 説明 |
| --- | --- |
| `TradeVolume` | 各エントリーブロックの数量。 |
| `MaxEntries` | 蓄積できる数量ブロックの最大数。 |
| `StopLossPips` | pips 単位の stop-loss 距離。 |
| `TakeProfitPips` | pips 単位の take-profit 距離。 |
| `TrailingStopPips` | pips 単位の trailing 距離。 |
| `UseBreakEven` | stop の break-even 移動を有効にします。 |
| `BreakEvenTriggerPips` | break-even が有効になる前に必要な利益。 |
| `BreakEvenOffsetPips` | break-even へ移動するときに追加される pips。 |
| `UseBollingerExit` | Bollinger band 接触でのエグジットを有効にします。 |
| `BollingerPeriod` / `BollingerDeviation` | Bollinger Bands の設定。 |
| `UseProfitMoneyTarget` / `ProfitMoneyTarget` | 絶対利益目標のスイッチと値。 |
| `UseProfitPercentTarget` / `ProfitPercentTarget` | 割合利益目標のスイッチと値。 |
| `EnableMoneyTrail` | 金額ベースの trailing stop を有効にします。 |
| `MoneyTrailTrigger` | 金額 trail が有効になる前に必要な利益。 |
| `MoneyTrailStop` | エグジット前に許容されるピークからの drawdown。 |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD 設定。 |
| `CandleType` | 実行時間枠。 |
| `SignalCandleType` | MACD シグナルに使用する時間枠。 |

## 使用上の注意

- 戦略は、商品の正しい `PriceStep` と `StepPrice` 値に依存します。起動前に銘柄を設定してください。
- 口座がポートフォリオ値 (`Portfolio.CurrentValue` または `Portfolio.BeginValue`) を報告しない場合、割合 take-profit は自動的に無視されます。
- C# ファイル内のすべてのコメントは、今後の保守を簡単にするため、取引ロジックを英語で記述しています。
