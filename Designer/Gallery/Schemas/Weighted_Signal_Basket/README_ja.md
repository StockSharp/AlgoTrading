# 期限付き指値を使う加重シグナルバスケット
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この図はRSIゾーン票とEMA位置票を−3から+3の1つのスコアに結合します。フラット時の閾値通過で確定終値に指値を登録し、Combination<Order>がその注文を12本のN valuesタイマーとOrder cancellationへ渡し、約定は1.2%/0.8%のPosition protectionを開始します。

![schema](schema.svg)

## 戦略の概要

- RSIが30未満なら+2、70超なら−2、中間ゾーンは0を加えます。
- CloseがEMA(20)より上なら+1、下なら−1で、Formulaが2つの加重票を合計します。
- 現在値とPrevious valueの比較が+1上抜けまたは−1下抜けを検出し、両側ともPosition == 0を要求します。
- 買いと売りは確定closeと数量1を共有し、Order出力はCombinationへ、MyTradeはPosition protectionへ進みます。
- N valuesは登録後12本の確定足を数え、Order cancellationが現在の未約定注文を取り消します。

## エントリーとエグジットの条件

- **ロングエントリー**: スコアが+1未満から+1以上になり、ポジションが空なら、Order registeringが確定closeに買い指値を置きます。
- **ショートエントリー**: スコアが−1超から−1以下になり、ポジションが空なら、Order registeringが確定closeに売り指値を置きます。
- **エグジット**: 約定入口は約定値から+1.2%と−0.8%で保護されます。未約定指値はOrderオブジェクトとして、N valuesが12本を数えた後Order cancellationへ渡されます。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 確定足間隔。月次リプレイ用にC#既定60分から5分へ適応します。 |
| RSI Length | 14 | RSI期間。C#既定21に対して本図は14です。 |
| EMA Length | 20 | EMA期間。C#既定50に対して本図は20です。 |
| RSI Weight | 2 | RSI売られ過ぎ・買われ過ぎ票の重みです。 |
| Trend Weight | 1 | closeのEMA相対位置票の重みです。 |
| Replay Signal Threshold | 1 | リプレイ境界。設計値2は文書化した選択肢として残ります。 |
| Cancel After N Candles | 12 | 未約定入口を取消す前に数える確定足です。 |
| Take Profit, % | 1.2 | Position protectionの利益率です。 |
| Stop Loss, % | 0.8 | Position protectionの損失率です。 |
| Order Volume | 1 | 各買い・売り指値入口の数量です。 |

## ダイアグラムの詳細

- 実行C#の既定は60分足、RSI(21)、EMA(50)、閾値2相当、4本のクールダウンで、足方向と中間RSIゾーンも採点します。本図は5分、14/20、2票、独立クールダウンなしに簡略化しています。
- レビュー済み設計は閾値2でした。しかし2票だけでは、3月リプレイで売買がなく、売られ過ぎRSIとEMA下の価格が互いに打ち消しました。そのため透明なリプレイ既定を1とし、2へ戻せるよう公開しています。
- 隣接READMEは元EAの8パターン、保留価格差、期限、保護を説明します。現在のC#は3つの採点系列と成行入口を実装し、期限・保護ブロックはありません。
- C#成行の代わりにclose指値を使うのは、Combination、N values、Order cancellationへ実注文周期を与えるためです。リプレイ銘柄に価格刻みがないため縮小を無効化します。
- C#のPosition <= 0 / >= 0と違い、本図はフラット時だけ入り反転しません。1.2/0.8の割合保護は教育用で、実行C#ロジックではありません。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
