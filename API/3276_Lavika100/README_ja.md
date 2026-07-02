# Lavika100戦略 (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Lavika100戦略**は、MetaTrader 5 エキスパートアドバイザー "Lavika  cent" を忠実に移植したものです。このシステムは、1 時間足 (H1) と 4 時間足 (H4) の RAVI momentum フィルターを組み合わせ、いつ取引を開くかを判断します。元の資金管理の選択肢 (固定ロットまたはリスク割合)、1 ポジションの規律、任意のシグナル反転、自動ストップ管理を維持します。StockSharp 版は高レベル API ガイドラインに従っています。ローソク足購読がワークフローを駆動し、インジケーターは binder 経由でアクセスされ、保護注文は `StartProtection` で設定されます。

## ワークフロー
1. **データ購読** - 戦略は実行時間枠として H1 ローソク足を購読し、トレンドフィルターとして H4 ローソク足を購読します。`SimpleMovingAverage` インジケーターは始値に適用され、MT5 の `iMA(..., PRICE_OPEN)` 呼び出しを再現します。
2. **RAVI momentum** - 各時間枠の 2 つの移動平均 (高速/低速) が "RAVI" 割合 `(fast - slow) / slow * 100` を生成します。取引を検討する前に、H1 の値が正である必要があります。
3. **トレンドパターン検出** - H4 の直近 4 つの RAVI 値を確認します。
   - 上昇シーケンス (`r0 > r1`, `r1 < r2`, `r2 < r3`) はロングシグナルを発動します。
   - 下降シーケンス (`r0 < r1`, `r1 > r2`, `r2 > r3`) はショートシグナルを発動します。これは、エキスパートが `Reverse` フラグ経由でしか方向を反転しなかったにもかかわらず、元コードの動作を再現しています。
4. **シグナル反転とフラット化** - `ReverseSignals` と `CloseOpposite` パラメーターに応じて、アルゴリズムは検出方向で開くか反転して開き、事前に反対ポジションを閉じます。
5. **資金管理** - 数量は `FixedVolume` から取得するか、`RiskPercent` メソッド (ポートフォリオ値 * 割合 / ストップ距離) によりリスクでスケーリングされます。
6. **保護** - stop-loss、take-profit、trailing stop、trailing step は、戦略開始時点でパラメーターがゼロでなければ `StartProtection` により有効化されます。

## 取引ルール
- **ロングエントリー** - H1 RAVI が正で、H4 系列が上昇パターンを示します。`CloseOpposite=true` の場合、買う前に既存のショートポジションを閉じます。
- **ショートエントリー** - H1 RAVI が正で、H4 系列が下降パターンを示します。`ReverseSignals=true` の場合、MT5 の "Reverse" トグルに合わせて方向を入れ替えます。
- **単一ポジション** - `OnlyOnePosition=true` では、フラットでないエクスポージャーがある限り、ポジションが閉じるまで追加エントリーをブロックします。
- **数量サイジング** - リスク割合モードは、商品の `PriceStep`/`StepPrice` ペアを使って価格距離を金額に変換し、`VolumeStep`、`VolumeMin`、`VolumeMax` を尊重します。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `H1CandleType` | 実行ロジックの時間枠 (デフォルト 1 時間)。 |
| `H4CandleType` | トレンドフィルターで使用する上位時間枠 (デフォルト 4 時間)。 |
| `H1FastPeriod` / `H1SlowPeriod` | H1 RAVI の移動平均長。 |
| `H4FastPeriod` / `H4SlowPeriod` | H4 RAVI の移動平均長。 |
| `StopLossPoints` | pip ベースポイントでの stop-loss 距離。 |
| `TakeProfitPoints` | pip ベースポイントでの take-profit 距離。 |
| `TrailingStopPoints` | trailing stop 距離。trailing を無効にするにはゼロに設定します。 |
| `TrailingStepPoints` | trailing 更新の最小ステップ。trailing が有効な場合は正である必要があります。 |
| `FixedVolume` | 固定モードで使うロットサイズ。 |
| `RiskPercent` | `MoneyMode` が `RiskPercent` と等しい場合にリスクにさらすポートフォリオ値の割合。 |
| `MoneyMode` | `FixedLot` と `RiskPercent` を切り替えます。 |
| `OnlyOnePosition` | 開けるポジションを 1 つだけにします。 |
| `ReverseSignals` | ロング/ショートの動作を反転します (EA 設定に合わせるためデフォルト true)。 |
| `CloseOpposite` | 新しい注文を出す前に反対ポジションを閉じます。 |

## 変換メモ
- pip 変換は MT5 エキスパートを模倣します。3 桁および 5 桁クォートでは `PriceStep` を 10 倍して pip サイズの増分を得ます。
- RAVI 履歴はカスタムコレクションを使わず、4 つの nullable フィールドだけで保存され、手動バッファーを禁じるリポジトリ制約を尊重しています。
- 資金管理はインジケーターの `GetValue` 呼び出しを避け、StockSharp の市場メタデータを使って割合リスクを数量へマッピングします。
- `StartProtection` は少なくとも 1 つの保護距離が正の場合にのみ呼び出され、バックテストとライブ取引で安全な実行を保証します。

## 使用のヒント
- `PriceStep`、`StepPrice`、`VolumeStep`、`VolumeMin`、`VolumeMax` が正しく設定された Forex 型の商品を用意してください。
- リスクベースのサイジングを使う場合は、ゼロでない `StopLossPoints` を定義してください。そうしないと計算数量はゼロになります。
- 元の EA には両方のパターンが買いフラグを設定するというロジック上の癖があったため、正確な取引を再現する必要がある場合は `ReverseSignals=true` を維持してください。
