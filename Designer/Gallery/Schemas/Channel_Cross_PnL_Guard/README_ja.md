# P&L保護付きチャネル交差ストラテジーダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、BTCUSDT@BNBFTの確定済み5分足から24本チャネルの中間値を計算し、200本のクールダウン後に終値と中間値の厳密な交差を取引します。TONUSDT@BNBFTの板情報はデータ準備完了を確認し、含みP&Lを検査するタイミングを供給します。1回限りの金額保護は、いずれかの設定しきい値に達するとまだ有効なエントリー注文の取消しを要求し、BTCポジションを決済します。

![schema](schema.svg)

## ストラテジー概要

- BTC Security変数は確定済み5分足の購読を設定します。成行注文、約定、ポジション決済、P&Lは選択したStrategy Securityに属するため、BTC Securityに合わせてBTCUSDT@BNBFTを設定する必要があります。
- Highest(24)はBTC足を受け取って高値を追跡し、Lowest(24)は安値を追跡します。算術中間値は`(Highest + Lowest) / 2`です。最初に利用できる判断は終値と中間値を保存して最初のクールダウンを開始し、注文を送信しません。
- 上向き交差には`Previous Close <= Previous Midpoint`と`Current Close > Current Midpoint`が必要です。下向き交差には`Previous Close >= Previous Midpoint`と`Current Close < Current Midpoint`が必要です。保存値は、クールダウンで拒否された足を含むすべての確定済みBTC足で更新されます。
- エントリーまたは反転が可能になるのは、厳密に後続する200本の確定済みBTC足が経過し、TON板情報イベントを少なくとも1回受信した後だけです。受理された交差は、成行注文を送信する前に200本の遅延を再開します。
- 約定駆動の符号付きラッチがBTCの管理状態を記録します。`-1`はショート、`0`はポジションなし、`1`はロングです。含みP&Lが`500`以上または`-300`以下になると1回限りの保護が作動し、その後の買いまたは売り約定で再度待機状態になります。

## エントリーと決済のルール

- **ロングエントリー**: 厳密な上向き交差が発生し、符号付きラッチがポジションなしまたはショートで、両方の準備ゲートが開き、クールダウンが完了している場合、成行買いを送信します。ポジションなしからのエントリーはBase BTC Volumeを使い、ショートからロングへの反転はその2倍を使います。
- **ショートエントリー**: 厳密な下向き交差が発生し、符号付きラッチがポジションなしまたはロングで、両方の準備ゲートが開き、クールダウンが完了している場合、成行売りを送信します。ポジションなしからのエントリーはBase BTC Volumeを使い、ロングからショートへの反転はその2倍を使います。
- **決済**: 条件を満たす反対交差は、個別の決済ではなく通常の1段階反転を実行します。これとは独立して、P&L保護は`P&L >= Profit Target`または`P&L <= -abs(Maximum Loss)`で作動し、一括取消要求と保存済みエントリー注文への個別取消要求を送り、その後に成行決済を要求します。この金額決済は交差クールダウンを再開しません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| BTC Security | BTCUSDT@BNBFT | 5分足購読に使う銘柄です。注文動作、約定、ポジション決済、P&LはStrategy Securityを使うため、同じ値を設定してください。 |
| TON Readiness Security | TONUSDT@BNBFT | データ準備ゲートを開きP&Lサンプリングを刻む板情報購読だけに使う銘柄です。その気配値はBTC注文にもP&L評価にも使いません。 |
| Candle Series | 00:05:00 | チャネル、厳密な交差判断、クールダウンのカウント、チャートに使うBTCUSDTの確定済み5分足系列です。 |
| Highest Length | 24 | Highestがチャネル上限を計算するために使うBTC足の本数です。 |
| Lowest Length | 24 | Lowestがチャネル下限を計算するために使うBTC足の本数です。 |
| Base BTC Volume | 1 | 成行エントリーの既定数量です。動作式は`Base BTC Volume * (1 + abs(latch))`なので、ポジションなしからのエントリーは基本数量、反転は基本数量の2倍を使います。 |
| Cooldown N | 200 | 初期化または受理された交差の後、別の交差が注文を送れるまでに必要な、厳密に後続する確定済みBTC足の本数です。 |
| Profit Target | 500 | 含みP&Lがこの値以上になると、1回限りの保護が取消と成行決済を要求します。 |
| Maximum Loss | 300 | 正の損失幅です。保護しきい値は`-abs(Maximum Loss)`で計算され、既定では`-300`です。 |

## ダイアグラム詳細

- BTCの[変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)は確定済み[ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)購読だけに値を渡します。TON変数は[板情報](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html)だけに値を渡します。最初のイベントが準備ラッチを書き込み、以後のイベントは最新の含みP&L値をサンプリングするタイミングにもなります。
- 2つの[インジケーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)ブロックが各BTC足を受け取ります。Highest(24)、Lowest(24)、中間値式、現在値ラッチ、前回値ラッチにより、確定足ごとに完全な終値とチャネルの判断を1回保持します。
- Delayブロックは最初の判断で開始し、交差が受理されるたびに再開します。現在の足は判断分岐が動く前にInputへ到達するため、再び許可されるのは後続する200本の確定済みBTC足の後だけです。拒否された交差でも保存済みの終値と中間値は置き換わります。
- 買いと売りの[注文登録](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/register.html)ブロックは、`Base BTC Volume * (1 + abs(latch))`の数量で成行注文を送信します。MyTrade出力は実際の約定から符号付き状態を書き込み、P&L保護を再度待機状態にします。
- P&L変化イベントとTON板情報イベントが最新の含みP&Lをサンプリングします。厳密なしきい値比較は共有待機ゲートへ入るため、その後のエントリー約定で再度待機するまで、`500`または`-300`への到達で生成できる保護動作は1回だけです。
- 保護は[注文の一括取消](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html)を呼び出し、保存した買いと売りのOrder参照を個別取消ブロックへ渡し、Base BTC Volumeで現在のBTCエクスポージャーを成行決済します。チャートはBTC足、Highest(24)、Lowest(24)、中間値、P&L、送信済み注文、すべてのストラテジー約定を受け取ります。

## 使用方法

`.json`ファイルをDesignerへインポートし、Strategy SecurityをBTCUSDT@BNBFTに設定し、足と板情報の履歴を使ってバックテスターで実行してから、ライブ取引の前にパラメーターまたはブロックを対象銘柄に合わせて調整してください。
