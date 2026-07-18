# BB Swing戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

**BB Swing戦略**は、MetaTraderの「BB SWING」エキスパートアドバイザーの忠実なポートです。2つの線形加重移動平均（LWMA）によって定義される主要なトレンドに沿ったボリンジャーバンドプルバックを取引します。高位タイムフレームのMomentumフィルターと非常に遅いMACDが、ポジションを開く前に反転の強さを確認するのを助けます。

## トレードロジック

1. `CandleType` タイムフレームの完成したローソク足のみで作業します。
2. 最近の極値とローソク足の胴体を検査するために最後の4つの完成したローソク足を追跡します。
3. 速いLWMAが（ロングの場合）遅いLWMAより上に、または（ショートの場合）下にとどまるのを待ちます。
4. 最後の3つの安値のいずれかがボリンジャーバンドの下限に触れている（ロングセットアップ）、または最高値のいずれかが上限バンドに触れている（ショートセットアップ）ことを確認します。
5. 前のローソク足がその前のローソク足より強い胴体を持つことを要求し、バンドから離れるMomentumを示します。
6. `MomentumCandleType` で計算されたMomentumでトレンドの強さを確認します。戦略はMomentum読み取りと100の絶対距離を測定します；距離は直近3つのMomentum値のいずれかで設定された買い/売り閾値を超える必要があります。
7. `MacdCandleType` タイムフレームで計算されたMACDで長期的な方向性を検証します。ロングエントリーはMACD主線がシグナル線より上にある間許可されます；ショートは逆の関係を必要とします。
8. すべての条件が揃ったとき、現在のマーチンゲールボリュームステップを使用して市場ポジションに入ります。

## ポジションサイジングとスケーリング

- `InitialVolume` は最初のエントリーボリュームを定義します。
- 追加のアドオンはそれぞれベースボリュームを `LotExponent` で乗算します（`volume = InitialVolume * LotExponent^n`）。
- `MaxTrades` は連続アドオン数を制限し、合計ポジションサイズが `InitialVolume * MaxTrades` を決して超えないようにします。

## エグジットと保護ルール

- 価格ステップで表された固定の `StopLoss` と `TakeProfit` 値。
- オプションのブレイクイーブンロジック（`EnableBreakEven`）で価格が `BreakEvenTrigger` ステップ進んだときにストップを `BreakEvenOffset` に移動します。
- 極値価格を `TrailingStop` ステップ追跡するクラシックトレーリングストップ（`EnableTrailingStop`）。
- 資金管理ツール：
  - `UseMoneyTakeProfit` は口座通貨での未実現利益が `MoneyTakeProfit` に達したときにポジションを決済します。
  - `UsePercentTakeProfit` は利益が初期資産の `PercentTakeProfit` パーセントに等しいときにポジションを決済します。
  - `UseMoneyTrailing` は利益トレールを有効にします：利益が `MoneyTrailTarget` を超えると、`MoneyTrailStop` の後退がエグジットを引き起こします。
- `UseEquityStop` はセッション中に記録された資産ピークに対する資産ドローダウンを監視します。`EquityRiskPercent` より大きいドローダウンはすべてのポジションを決済します。
- オプションの `CloseOnMacdCross` は現在のポジション方向に反してMACD主線がシグナル線を交差するたびにエグジットします。

すべての保護アクションはポジション全体を中立化するために成行注文（`BuyMarket` / `SellMarket`）に依存します。

## パラメーター

| 名前 | 説明 |
|------|-------------|
| `InitialVolume` | 最初のエントリーに使用されるベーストレードボリューム。 |
| `LotExponent` | スケーリング時の各追加エントリーのボリュームに適用される乗数。 |
| `MaxTrades` | 任意の時点で許可される連続アドオンの最大数。 |
| `TakeProfit` | 価格ステップで表されたテイクプロフィット。 |
| `StopLoss` | 価格ステップで表されたストップロス。 |
| `FastMaPeriod` | 典型価格で計算された速いLWMAの期間。 |
| `SlowMaPeriod` | 典型価格で計算された遅いLWMAの期間。 |
| `MomentumLength` | Momentum計算で使用されるバーの数。 |
| `MomentumBuyThreshold` | 高位タイムフレームMomentumがロングトレードを検証するための100からの最小距離。 |
| `MomentumSellThreshold` | 高位タイムフレームMomentumがショートトレードを検証するための100からの最小距離。 |
| `EnableBreakEven` | ブレイクイーブンストップ移動を有効化。 |
| `BreakEvenTrigger` | ブレイクイーブン移動を発動するために必要な価格ステップ。 |
| `BreakEvenOffset` | ブレイクイーブン発動後にストップに適用されるオフセット。 |
| `EnableTrailingStop` | 価格ステップでのクラシックトレーリングストップを有効化。 |
| `TrailingStop` | ステップで表されたトレーリングストップのサイズ。 |
| `UseMoneyTakeProfit` | 口座通貨での固定利益確定を有効化。 |
| `MoneyTakeProfit` | `UseMoneyTakeProfit` がアクティブのときにポジションを決済する通貨での利益。 |
| `UsePercentTakeProfit` | 資産パーセントベースの利益確定を有効化。 |
| `PercentTakeProfit` | `UsePercentTakeProfit` がアクティブのときにエグジットを引き起こす初期資産の割合。 |
| `UseMoneyTrailing` | 目標利益達成後の資金ベーストレーリングを有効化。 |
| `MoneyTrailTarget` | マネートレーリングロジックを有効化する利益レベル。 |
| `MoneyTrailStop` | 発動後の許容される最大後退（通貨）。 |
| `UseEquityStop` | 浮動ドローダウンが閾値を超えたときのポジション決済を有効化。 |
| `EquityRiskPercent` | 許可される最大資産ドローダウン（パーセント）。 |
| `CloseOnMacdCross` | MACDベースのエグジットフィルタリングを有効化。 |
| `CandleType` | シグナル計算に使用されるプライマリタイムフレーム。 |
| `MomentumCandleType` | Momentumフィルターに使用される高位タイムフレーム。 |
| `MacdCandleType` | MACDエグジットフィルターで使用される非常に遅いタイムフレーム。 |

## 注意事項

- 戦略は完成したローソク足のみを処理します；バー内では反応しません。
- すべてのストップと目標計算は接続されたexchangeが報告するインストゥルメントの価格ステップを使用します。正確なリスク管理のために `PriceStep` が正しく設定されていることを確認してください。
- 資金ベースおよび資産ベースの保護はStockSharpで利用可能な戦略ポートフォリオ統計に依存します。テスターモードで実行する場合は、ポートフォリオフィードが有効になっていることを確認してください。
- 元のMQLエキスパートとは異なり、このC#実装は方向ごとに単一の集計ポジションを維持します。スケーリングは複数の離散チケットを開く代わりに集計ポジションを増加させます。
- ボリンジャーバンドは元のコードに合わせて典型価格で固定長20、幅2標準偏差を使用します。
