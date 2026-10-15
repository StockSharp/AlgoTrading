# 2資産・各自移動平均ストラテジー図
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この図は、BTCUSDT@BNBFTとTONUSDT@BNBFTの確定済み15分足を同期し、各終値をその銘柄について計算した期間20の単純移動平均と比較して、逆向きの関係からBTCUSDTのロングとショートのエクスポージャーを管理します。約定で更新される符号付き状態ラッチ、1回の成行アクションによる反転、通常決済、ローカル固定2%ストップで一連の処理を構成します。

![schema](schema.svg)

## 戦略の概要

- 個別の銘柄変数は、BTCUSDTとTONUSDTの足購読だけを設定します。注文アクションとStrategy tradesストリームは選択中のStrategy Securityを使用するため、Traded Securityパラメーターと一致するBTCUSDT@BNBFTを設定する必要があります。
- 確定済み15分足だけがSyncブロックに入ります。同期された各ペアは、一方の分岐にBTC CloseとBTC SMA(20)、もう一方にTON CloseとTON SMA(20)を渡し、両方の平均が形成されてから判定を始めます。
- ロング関係は`BTC Close < BTC SMA(20)`と`TON Close > TON SMA(20)`の両方を厳密に要求します。ショート関係は`BTC Close > BTC SMA(20)`と`TON Close < TON SMA(20)`の両方を厳密に要求します。等値はどちらの完全な関係も満たしません。
- 数値型の符号付き状態ラッチは、この図が管理するBTC状態を記録します。`-1`はショート、`0`はフラット、`1`はロングです。フラットでは完全な関係により1単位の成行エントリーを送信します。反対側の状態ではアクション数量が2単位となり、1回の成行アクションで既存の1単位を閉じ、新しい方向へ1単位を建てます。
- 完全な反対関係は通常決済より優先されます。それ以外では、BTCが自身の平均に対して厳密に決済側にあるとき、1単位のReduceOnly成行アクションで現在の方向を閉じます。同期判定は、保存されたBTC終値がローカル固定2%成行ストップへ渡される前に完了します。

## エントリーとエグジットの条件

- **ロングエントリー**: 同期された確定足ペアが`BTC Close < BTC SMA(20)`と`TON Close > TON SMA(20)`を満たすと、買いゲートはフラットまたはショートのラッチ状態を受け入れます。フラットからはVolume 1のNoCondition成行買いを送り、1単位ショートからはVolume 2を送って直接1単位のBTCロングへ反転します。
- **ショートエントリー**: 同期された確定足ペアが`BTC Close > BTC SMA(20)`と`TON Close < TON SMA(20)`を満たすと、売りゲートはフラットまたはロングのラッチ状態を受け入れます。フラットからはVolume 1のNoCondition成行売りを送り、1単位ロングからはVolume 2を送って直接1単位のBTCショートへ反転します。
- **エグジット**: BTC CloseがBTC SMA(20)を厳密に上回り、完全なショート関係がない場合、ロングを1単位のReduceOnly成行売りで閉じます。BTC CloseがBTC SMA(20)を厳密に下回り、完全なロング関係がない場合、ショートを1単位のReduceOnly成行買いで閉じます。これらの否定確認により、完全な反対関係は2単位の反転分岐へ渡されます。ローカル保護も固定2%成行ストップでどちらの方向も閉じられます。Take Profit 0は利益目標を無効にします。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | 取引対象側の足購読だけに使用する銘柄です。すべての注文アクションとStrategy tradesストリームがStrategy Securityを使用するため、同じBTCUSDT@BNBFTをStrategy Securityに設定してください。 |
| Signal Security | TONUSDT@BNBFT | 2番目の足購読だけに使用する銘柄です。その価格と平均の関係は判定に寄与しますが、この変数を宛先とする注文アクションはありません。 |
| BTC Candles Series | 00:15:00 | 同期、BTC Close、BTC SMA(20)、保護判定、チャートに使用するBTCUSDTの確定済み15分足シリーズです。 |
| TON Candles Series | 00:15:00 | 同期、TON Close、TON SMA(20)、チャートに使用するTONUSDTの確定済み15分足シリーズです。 |
| BTC SMA Length | 20 | 同期された確定済みBTCUSDT足から計算するSimpleMovingAverageの期間です。 |
| TON SMA Length | 20 | 同期された確定済みTONUSDT足から計算するSimpleMovingAverageの期間です。 |
| Base Volume | 1 | 成行エントリーの既定数量です。アクション数量は`Base Volume * (1 + abs(latch))`です。そのためフラットからのエントリーはBase Volume、反転はBase Volumeの2倍となり、既定値ではVolume 1とVolume 2です。 |
| Take Profit | 0 | 絶対値ゼロはtake-profit保護を無効にします。 |
| Stop Loss | 2% | 保護対象の約定価格から不利な方向への割合で、この距離に達するとstop-lossが作動します。 |
| Trailing Stop Loss | false | 無効のため、2%ストップは固定され、有利な価格変動には追従しません。 |
| Use Market Orders | true | 有効のため、作動したストップは成行注文で保護対象のエクスポージャーを閉じます。 |

## ダイアグラムの詳細

- 銘柄型の2つの[変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)ブロックは、それぞれ対応する[ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックだけに値を渡します。[同期](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/sync.html)ブロックが`00:15:00`で確定済みストリームを揃えてから、各分岐を判定チェーンへ渡します。
- 同期された各足はCloseと形成済みの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)値に分けられます。4つの厳密比較ブロックが、各銘柄のCloseとそれぞれのSMA(20)から、逆向きのロング関係とショート関係を組み立てます。
- 数値型Unit変数が符号付き状態ラッチを保持します。買い約定後は`1`、売り約定後は`-1`、通常決済または保護決済後は`0`を書き込みます。状態比較はフラットからのエントリーと反対側からの反転を許可し、数量式は`Base Volume * (1 + abs(latch))`です。
- ロング関係とショート関係のゲートは、NoCondition、MarketOrderに設定した[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)アクションを動かします。通常決済は別々のReduceOnly成行アクションが処理します。各通常決済ゲートは完全な反対関係がfalseであることも要求するため、同じ同期ペアで反転と1単位の決済が同時に要求されることはありません。
- [ポジション保護](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)は、エントリー、反転、通常決済の約定を受け取ります。Take Profitは`0`、Stop Lossは`2%`、Trailing Stop Lossは`false`、Use Market Ordersは`true`で、保護はローカルで動作します。同期されたBTC終値はいったん保存され、両方のシグナル分岐がそのペアの判定を完了してから保護へ渡されます。
- チャートは同期されたBTCUSDTとTONUSDTの足ストリーム、BTC SMA(20)、TON SMA(20)、stop-loss注文ストリーム、Strategy tradesからの全BTCUSDT約定を受け取ります。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
