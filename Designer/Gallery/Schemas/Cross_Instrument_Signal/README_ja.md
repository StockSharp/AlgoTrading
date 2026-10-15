# 銘柄間シグナル戦略図
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定したTONUSDT@BNBFTとBTCUSDT@BNBFTの4時間足を同期します。TONUSDTは期間20の変化率シグナルを提供し、BTCUSDTは自身の期間20の単純移動平均フィルターを提供します。Strategy SecurityをBTCUSDT@BNBFTに設定して動かす構成で、内部の数値`0/1`フラット/ロング状態ラッチ、共有エントリー許可状態、約定駆動の保護がロング限定のエクスポージャーを管理します。

![schema](schema.svg)

## 戦略の概要

- 2つの独立した銘柄変数は、TONUSDTとBTCUSDTの4時間足購読だけを設定します。確定足は判断前に整列されるため、TONのモメンタム値とBTCのトレンドフィルターは常に同じ同期区間に属します。
- TON ROC(20)はゼロより上で強気となり、ゼロ以下では決済条件になります。BTCUSDTのCloseがSMA(20)以上ならエントリー可能となり、CloseがSMA(20)未満なら決済条件になります。
- ロングエントリーには、`TON ROC(20) > 0`、`BTC Close >= BTC SMA(20)`、内部状態ラッチがフラット、共有`Cooldown is ready`状態がエントリーを許可、という4条件の同時成立が必要です。外部で組んだANDが数量1のNoCondition成行買いを起動します。
- エントリーアクションは共有許可を解除してEntry Cooldown Nを開始し、裁量シグナル売りは同じ許可を解除してSignal-exit Cooldown Nを開始します。対応するタイマーが同期足ペア8組後に許可を戻します。世代チェックは、より新しいリセット後に古いタイマーが完了しても無効にします。決済はこの状態を待たず、保護決済もリセットしません。
- BTCUSDTの成行買い約定はラッチをロングへ切り替え、2%の利確と固定された非トレーリングの2.5%損切りを有効にします。裁量決済の約定、またはTake/Stopの起動と約定はラッチをフラットへ戻します。ショートは建てません。

## エントリーとエグジットの条件

- **ロングエントリー**: 同期した確定4時間足の組で、TON ROC(20)がゼロより上、BTC CloseがBTC SMA(20)以上、ラッチがフラット、共有`Cooldown is ready`状態がエントリーを許可する場合、外部のエントリーANDが数量1のNoCondition成行買いを起動します。このアクションは選択中のStrategy Securityを取引するため、Traded Security足パラメーターと同じBTCUSDT@BNBFTに設定する必要があります。
- **ショートエントリー**: ショートエントリーはありません。裁量売りはReduceOnly、MarketOrder、数量1を使うため、選択中のStrategy Securityのエクスポージャーを減らすことしかできません。TakeとStopの保護もロングエクスポージャーを閉じます。
- **エグジット**: ラッチがロングを示すとき、`TON ROC(20) <= 0`または`BTC Close < BTC SMA(20)`が、共有クールダウン状態を待たずにReduceOnly成行売りを起動します。2%の利確または固定2.5%の損切りでも成行でエクスポージャーを閉じます。損切りは追従しません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | 取引対象側の足購読だけに使う銘柄です。アクションとストラテジー約定はStrategy Securityを使うため、選択中のStrategy Securityも同じBTCUSDT@BNBFTに設定します。 |
| Signal Security | TONUSDT@BNBFT | シグナル側の足購読だけに使う銘柄です。そのモメンタムはシグナルに寄与しますが、この変数を宛先とするアクションはありません。 |
| BTC Candles Series | 04:00:00 | Close、SMA(20)、状態判断、チャートに使う確定4時間BTCUSDT足シリーズです。 |
| TON Candles Series | 04:00:00 | ROC(20)と同期判断に使う確定4時間TONUSDT足シリーズです。 |
| BTC SMA Length | 20 | 確定BTCUSDT足から計算するSimpleMovingAverageの期間です。 |
| TON ROC Length | 20 | 確定TONUSDT足から計算するRateOfChangeの期間です。 |
| ROC Threshold | 0 | エントリー用の正モメンタム状態と決済用の非正シグナル状態を分けるゼロ水準です。 |
| Entry Cooldown N | 8 | エントリーアクション用タイマーが共有エントリー許可を戻せるまでに数える同期足ペア数です。 |
| Signal-exit Cooldown N | 8 | 裁量シグナル決済用タイマーが共有エントリー許可を戻せるまでに数える同期足ペア数です。 |
| Order Volume | 1 | 選択中のStrategy Securityに対するNoCondition成行買いとReduceOnly成行売りが共用する固定数量です。 |
| Take Profit | 2% | エントリー約定価格からの上昇率で、利確保護を有効にします。 |
| Stop Loss | 2.5% | エントリー約定価格からの下落率で、損切り保護を有効にします。 |
| Trailing Stop Loss | false | 無効のため、2.5%損切りは固定され、有利な価格変動に追従しません。 |
| Use Market Orders | true | 有効のため、利確と損切りは成行注文でポジションを閉じます。 |

## ダイアグラムの詳細

- 銘柄型の個別[変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)ブロックは、TONUSDT@BNBFTとBTCUSDT@BNBFTの独立した足購読だけに接続します。注文アクションとストラテジー約定はStrategy Securityを使います。Traded Security足パラメーターと一致するよう、こちらにもBTCUSDT@BNBFTを選択します。
- 2つの[ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定4時間足だけを出力します。[同期](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/sync.html)ブロックが`04:00:00`間隔で2つのストリームを組にしてから判断チェーンへ渡します。
- 一方の[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックはTONUSDTのRateOfChange 20を、もう一方はBTCUSDTのSimpleMovingAverage 20を計算します。比較ブロックがROCの正と非正の状態、およびBTC Closeが平均以上か未満かを表します。
- 数値Unitの[変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)が状態ラッチとして動きます。`0`はフラット、`1`はロングです。Buy MyTradeが`1`を書き込み、裁量売りのMyTradeとTake/Stopの起動およびMyTradeイベントが`0`を書き込みます。論理条件ブロックがこの状態を同期シグナルに組み合わせます。
- アクション別の2つのN値タイマーはEntry Cooldown NとSignal-exit Cooldown Nを8に保持します。どちらのアクションも1つの共有エントリー許可を解除し、そのタイマーが同期足ペア8組後に`Cooldown is ready`を戻せます。世代チェックは以前のタイマーによる古い完了を破棄します。外部のフラット時エントリーANDにはクールダウン入力が1つだけあります。NoCondition、MarketOrder、数量1の買い[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)を起動し、シグナル決済はその入力なしで別のReduceOnly、MarketOrder、数量1の売りを起動します。
- [ポジション保護](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)は買いと裁量決済の約定を受け取ります。買い約定はTake Profit `2%`と固定Stop Loss `2.5%`を有効にし、決済約定は古い保護状態を消去します。Trailing Stop Lossは`false`、Use Market Ordersは`true`です。チャートは同期した2つの足ストリーム、BTC SMA(20)、TON ROC(20)、Take/Stop注文ストリーム、ストラテジー約定からの全BTC約定を受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
