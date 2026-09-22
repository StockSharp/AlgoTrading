# 単一銘柄SMMAバイアス戦略図
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

旧ギャラリー名とは異なり、この図はバスケットではありません。現在のVectorStrategyコードどおり、単一銘柄、4時間足の高速・低速平滑移動平均、ネット反転、絶対金額の含み損益による終了を使います。

![schema](schema.svg)

## 戦略の概要

- 確定4時間足がSMMA(3)とSMMA(7)へ入り、上下関係が強気・弱気バイアスを決めます。
- 一度だけ動くFlagがN valuesを開始し、最初の確定8本では取引しません。
- 強気はPosition <= 0で買い、弱気はPosition >= 0で売れます。
- 数量はabs(Position)+基本数量で、原典の決済と新規を1回のネット反転にまとめます。
- 含み損益が口座通貨で+5000または-300000に達するとP&L changeが閉じます。

## エントリーとエグジットの条件

- **ロングエントリー**: ウォームアップ後、高速SMMAが低速SMMAより上でPositionがゼロまたはショートなら、Modify positionが成行買いし、ショートを閉じて基本1単位のロングを残します。
- **ショートエントリー**: ウォームアップ後、高速SMMAが低速SMMAより下でPositionがゼロまたはロングなら、Modify positionが成行売りし、ロングを閉じて基本1単位のショートを残します。
- **エグジット**: 反対バイアスは直接ポジションを反転します。また含み損益が5000以上または-300000以下なら成行ClosePositionを実行し、同じ傾向が続けば後の足で再入場できます。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candle Time Frame | 04:00:00 | 両方の平滑移動平均とポジション判断に使う確定足時間枠。 |
| Fast SMMA Length | 3 | 高速SmoothedMovingAverageに含める値の数。 |
| Slow SMMA Length | 7 | 低速SmoothedMovingAverageに含める値の数。 |
| MA Shift Warmup | 8 | トレンド入場を許可する前に待つ初期確定足数。 |
| Base Volume | 1 | フラットからの入場またはネット反転後に残す数量。 |
| Profit Target, money | 5000 | ClosePositionを起動する口座通貨の含み利益。 |
| Loss Limit, money | -300000 | 口座通貨の含み損失境界。負の値にします。 |

## ダイアグラムの詳細

- 原典のGetWorkingSecuritiesは(Security, CandleType)だけを返すため、Index、Sync、第2銘柄、バスケット確認は意図的にありません。
- ProfitPercent 0.5とLossPercent 30をテスト開始残高1,000,000から+5000と-300000へ換算しています。
- Designerは含み損益の金額を出しますが開始残高を出さないため、割合を明示的な口座通貨額にしています。
- ウォームアップはprocessedBarsと同じく最初の確定8本を数え、取引開始前にSMMA(7)が形成されます。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
