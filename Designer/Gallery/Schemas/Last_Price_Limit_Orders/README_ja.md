# Level1指値によるLast Price平均回帰
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

本例の中心はOrder registeringとLevel1約定です。よくあるEMA乖離回帰を成行可能な指値で表現します。歴史的な戦略名とは異なり、判断はすべて確定4時間足から行い、ティック信号経路はありません。

![schema](schema.svg)

## 戦略の概要

- 確定4時間足終値をEMA(20)へ入力し、その上下0.5%境界と比較します。
- フラット時、下境界より低い終値で買い、上境界より高い終値で売りを要求します。
- ロングは終値がEMA以上へ戻ると退出し、ショートはEMA以下へ戻ると退出します。
- 各足で買い用best ask、売り用best bidを保存し、正価格ゲートで気配前の登録を防ぎます。
- 入退出は同じ数量1を使うため、本図が開いたポジションは1回の反対約定でフラットになります。

## エントリーとエグジットの条件

- **ロングエントリー**: Position == 0かつClose < EMA × (1 − 0.5/100)でbest askに買い指値を置き、スプレッドを越えてBuyMarket同様の約定を狙います。
- **ショートエントリー**: Position == 0かつClose > EMA × (1 + 0.5/100)でbest bidに売り指値を置き、SellMarket同様の約定を狙います。
- **エグジット**: Position > 0かつClose >= EMAでは共通sellがbest bidで1単位売り、Position < 0かつClose <= EMAではbuyがbest askで1単位買います。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candle Time Frame | 04:00:00 | EMAと全判断に使う確定足周期で、C#既定値と同じです。 |
| EMA Period | 20 | ExponentialMovingAverageに含める終値数です。 |
| Entry Distance, % | 0.5 | フラットから入るために必要なEMA乖離率です。 |
| Shared Entry/Exit Volume | 1 | 入退出指値で共用する数量です。 |

## ダイアグラムの詳細

- C#は入退出とも成行です。本図はOrder registeringを示すため、best ask買い・best bid売りの成行可能指値を使います。
- 受動型ならbest bid買い・best ask売りですが、残存注文にはOrder cancellationまたはreplacementが必要です。
- CloseとEMAは同じ確定足から来ます。変数がEMA更新後に終値を放出し、新終値と旧EMAの比較を防ぎます。
- 共通数量はStrategy.Volumeを再現します。外部または異なる大きさのポジションは固定1単位で必ずしもフラットになりません。
- 発想はMA_Deviationと重複し、本例固有の題材はLevel1気配の採取と成行可能指値の実行です。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
