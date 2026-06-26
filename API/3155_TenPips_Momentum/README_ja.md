# TenPips Momentum戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**TenPips戦略**は、MetaTraderの「10PIPS」エキスパートアドバイザーのStockSharpポートです。トレーディングタイムフレームで計算された典型価格 `(H + L + C) / 3` の速い/遅い線形加重移動平均と、マルチタイムフレームのMomentum確認、マクロ（月次）MACDフィルターを組み合わせています。変換は元の資金管理モジュールを再現し、ブレイクイーブン保護、pipsベースのトレーリング、資産/絶対利益目標を含みます。

## シグナルロジック

1. **プライマリタイムフレーム**（パラメーター `CandleType`、デフォルト15分）は典型価格 `(H + L + C) / 3` で計算された速い/遅いLWMAに使用される価格ストリームを供給します。
2. **高位タイムフレームのMomentum**（`MomentumCandleType`、デフォルト1時間）はStockSharpのMomentum差をMetaTraderの比率に変換します。最後の3つの完成したバーにわたる `100` からの絶対距離がトレードを発動するために `MomentumThreshold` を超える必要があります。
3. **マクロMACDフィルター**（`MacdCandleType`、デフォルト30日ローソク足でMetaTraderの月次期間を近似）は、買いではMACD主線がシグナル線を上回り、売りでは下回ることを必要とします。

前のローソク足が以下の場合にロングポジションが開かれます：
- 速いLWMAを下回った後でその上で引けた、
- 速いLWMAが遅いLWMAの上にある、
- 直近3つのMomentum読み取りのいずれかが `MomentumThreshold` を満たす、
- マクロMACDが強気。

ショートポジションは対称的な条件を使用します（前のクローズが速いLWMAを下回る、速いが遅いを下回る、Momentumが閾値を超える、MACDが弱気）。

StockSharpはネットポジションモデルで動作するため、ポートは各サイドに最大1つの集計ポジションを開きます。ショート中に買いを送ると自動的にショート部分が決済され、要求されたロングボリュームが残ります。

## リスクと資金管理

- **保護距離** – `StopLossPips` と `TakeProfitPips` はインストゥルメントの `PriceStep` を使ってMetaTrader pipsを価格オフセットに変換します。いずれかの境界に達した場合、戦略は成行注文でポジション全体を決済します。
- **トレーリングストップ** – `TrailingStopPips` はエントリー以来の最高値（ロング）または最安値（ショート）をフォローします。
- **ブレイクイーブン** – 有効な場合、`BreakEvenTriggerPips` がストップを起動し、エントリーにオプションの `BreakEvenOffsetPips` を加えた値にシフトします。
- **資金目標** – `UseMoneyTakeProfit`、`UsePercentTakeProfit`、`EnableMoneyTrailing` のトリオがEAの `TP_In_Money`、`TP_In_Percent`、残高ベースのトレーリングロックを再現します。未実現PnLはローソク足クローズごとに測定されます。
- **資産ストップ** – `UseEquityStop` と `EquityRiskPercent` は、資産ピークからのドローダウンが閾値を超えるとポジションを決済することで元の `UseEquityStop`/`TotalEquityRisk` 保護を実装します。
- **MACDエグジットフラグ** – `UseMacdExit` はEAの `Exit` スイッチを反映し、マクロMACDがトレードに反転したときにポジションを早期決済します。

## パラメーター

| パラメーター | デフォルト | 説明 |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | 成行注文に使用するネットポジションボリューム（MetaTraderのロットサイズ相当）。 |
| `CandleType` | `15m` タイムフレーム | 速い/遅いLWMAとトレード実行のプライマリタイムフレーム。 |
| `MomentumCandleType` | `1h` タイムフレーム | Momentum確認を供給する高位タイムフレームのローソク足。 |
| `MacdCandleType` | `30d` タイムフレーム | MACD確認のためのマクロタイムフレーム（月次近似）。 |
| `FastMaPeriod` | `8` | 速い線形加重移動平均の期間。 |
| `SlowMaPeriod` | `50` | 遅い線形加重移動平均の期間。 |
| `MomentumPeriod` | `14` | Momentum比率のルックバック。 |
| `MomentumThreshold` | `0.3` | 高位タイムフレームの直近3バーで必要な `100` からの最小絶対距離（MetaTrader Momentum）。 |
| `StopLossPips` | `20` | MetaTrader pips単位の保護ストップロス。無効にするには0に設定。 |
| `TakeProfitPips` | `50` | MetaTrader pips単位の保護テイクプロフィット。無効にするには0に設定。 |
| `TrailingStopPips` | `40` | pips単位のトレーリングストップ距離（0でトレーリング無効）。 |
| `UseBreakEven` | `true` | ブレイクイーブン移動動作を有効化。 |
| `BreakEvenTriggerPips` | `30` | ブレイクイーブン発動前に必要な利益（pips）。 |
| `BreakEvenOffsetPips` | `30` | 発動後にブレイクイーブンストップに追加される余分なpips。 |
| `UseMoneyTakeProfit` | `false` | 絶対利益目標 `MoneyTakeProfit` に達したときにポジションを決済。 |
| `MoneyTakeProfit` | `10` | 口座通貨で表された利益目標。 |
| `UsePercentTakeProfit` | `false` | 初期資産の `PercentTakeProfit` パーセントを獲得した後にポジションを決済。 |
| `PercentTakeProfit` | `10` | 開始資産に基づくパーセント目標。 |
| `EnableMoneyTrailing` | `true` | `MoneyTrailTarget` / `MoneyTrailStop` を使った残高ベースのトレーリングストップを有効化。 |
| `MoneyTrailTarget` | `40` | マネートレールを発動させる前に必要な利益（通貨）。 |
| `MoneyTrailStop` | `10` | マネートレール発動後の許容される後退。 |
| `UseEquityStop` | `true` | 資産ドローダウン保護を有効化。 |
| `EquityRiskPercent` | `1` | フラットポジションを強制する前の資産ピークからの最大ドローダウン。 |
| `UseMacdExit` | `false` | マクロタイムフレームからの反対のMACDシグナルでポジションを決済。 |

## 実装上の注意

- pip変換はEAロジックに従います：ブローカーのティックサイズが `0.00001` または `0.001` の場合、1 pipは10ティックに相当します；それ以外の場合は生の `PriceStep` が使用されます。
- StockSharpのMomentumインジケーターは価格差を出力します。戦略は `MomentumThreshold` を適用する前に `(Close / Close(period) * 100)` のMetaTrader比率に変換します。
- ポートはネッティング環境で動作するため、EAのマルチチケットマーチンゲール（`IncreaseFactor`、`LotExponent`、`Max_Trades`）を再現しません。代わりに、ロングとショートポジションを切り替える際に自動的に注文ボリュームを調整します。
- 保護的な出口と利益管理は成行注文を送信し、オープンチケットを変更する際の元のアドバイザーの動作と一致します。
- 可視化が利用可能な場合、チャートには処理されたインジケーター（速いLWMA、遅いLWMA、Momentum、MACD）が表示されます。

## 使用方法

1. EAが使用するMetaTraderチャートと高位タイムフレームに一致するようにローソク足タイムフレームを設定します。
2. pip ベースのリスクパラメーターをインストゥルメントのポイントサイズに合わせます。ゼロは対応するコンポーネントを無効にします。
3. リスク設定に応じてマネー/パーセント目標、資産ストップ、MACDエグジットを有効/無効にします。
4. 戦略を起動します。3つの必要なタイムフレームにサブスクライブし、元のルールに従ってポジションを管理し、残高ベースまたは資産保護によって引き起こされた保護的な出口を記録します。
