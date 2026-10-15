# RSI・Momentum指値セッション戦略図
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この図はRSI(14)、Momentum(14)、終日Working timeフィルター、管理対象の保留指値を組み合わせます。売られ過ぎかつ弱い勢いでは始値の下に買い、買われ過ぎかつ強い勢いでは上に売りを置き、信号消滅時に自側注文を明示的に取り消します。

![schema](schema.svg)

## 戦略の概要

- 確定5分足がRSI、Momentum、エントリー用始値、保護判定用終値を供給します。
- Working timeは00:00から23:59までを許可し、元コードの実質終日セッションを保ちながら時間ブロックを設定可能に示します。
- RSIが30未満、Momentumが1未満、Position <= 0で買い、RSIが70超、Momentumが1超、Position >= 0で売りを許可します。
- 一回限りのフラグが各信号期間の注文を最大1本にし、失効した自側または反対側注文を取り消します。
- 指値約定後は絶対利確35・損切り8で管理し、確定終値を保護の価格入力へ接続します。

## エントリーとエグジットの条件

- **ロングエントリー**: セッション内でRSI < 30、Momentum < 1、Position <= 0ならOpenPrice − 25に数量1の買い指値を登録し、先に有効な売りを取り消します。
- **ショートエントリー**: セッション内でRSI > 70、Momentum > 1、Position >= 0ならOpenPrice + 25に数量1の売り指値を登録し、先に有効な買いを取り消します。
- **エグジット**: 保護は絶対利益35または損失8で決済します。RSI、Momentum、ポジション条件のいずれかが無効なら買い指値を取り消し、売り側も対称です。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 確定足間隔。5分はリプレイ適応で、C#既定は15分です。 |
| RSI Period | 14 | RelativeStrengthIndexが使う値の数です。 |
| Momentum Period | 14 | Momentumが使う値の数です。 |
| Session Start | 00:00:00 | Working time開始時刻です。 |
| Session End | 23:59:00 | Working time終了時刻。23:59で終日動作を保ちます。 |
| RSI Buy Threshold | 30 | 買いにはRSIがこの値未満である必要があります。 |
| RSI Sell Threshold | 70 | 売りにはRSIがこの値を超える必要があります。 |
| Momentum Threshold | 1 | 買いはMomentumが未満、売りは超える必要があります。 |
| Limit Offset, price units | 25 | リプレイでOpenPriceから加減する絶対距離です。 |
| Order Volume | 1 | 各保留指値の数量です。 |
| Take Profit, price units | 35 | 約定価格からの絶対有利距離です。 |
| Stop Loss, price units | 8 | 約定価格からの絶対不利距離です。 |

## ダイアグラムの詳細

- C#の既定足は15分です。本図は十分な信号周期を表示するためリプレイで5分足を使い、両指標の期間14は維持します。これは明示的な標本化適応です。
- 元コードの距離は5 × PriceStepです。本図は価格刻みを別ブロックで取得しないため絶対25単位を使います。これはギャラリー実行値であり元の既定値ではありません。
- 指値はProcessCandleと同じくOpenPriceから計算します。ClosePriceは分離され、Position protectionの価格更新だけに使います。
- 元の保護距離は35 × PriceStepと8 × PriceStepです。本図は35と8をパーセントではなく絶対価格単位として保持します。
- 一回限りのフラグが有効注文チェックを再現し、RSI、Momentum、側別ポジション条件が無効になるとリセットされます。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
