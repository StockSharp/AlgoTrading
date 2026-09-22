# EMAクロス追従指値エントリー図
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この図はFranks4HourLimitOrdersStrategyのEMA(12)/EMA(26)交差とMomentum(10)確認を保ち、執行を可視化します。指値はシグナル足終値から始まり、方向が有効な間はOrder replacingで後続終値を追い、逆交差で取り消されます。

![schema](schema.svg)

## 戦略の概要

- 確定足が2本のEMAとMomentumへ入り、平均の順序が本当に変わった時だけCrossingが出力します。
- 上交差は正のMomentumとPosition <= 0、下交差は負のMomentumとPosition >= 0を必要とします。
- Order registeringは価格刻み丸めなしでシグナル足終値に最初の指値を置きます。
- CombinationはOrder replacingが返す最新注文を保持し、更新と取消が現行オブジェクトを対象にします。
- EMAとMomentumが同じ側を確認する間だけ置換し、無条件の撤回・再登録を防ぎます。

## エントリーとエグジットの条件

- **ロングエントリー**: EMA(12)がEMA(26)を上抜け、Momentumが正、Positionがゼロまたはショートなら、終値に買い指値を置きます。abs(Position)+1で平ショートと新規ロングを1回のネット反転にします。
- **ショートエントリー**: EMA(12)がEMA(26)を下抜け、Momentumが負、Positionがゼロまたはロングなら、同じネット反転数量で終値に売り指値を置きます。
- **エグジット**: 逆EMA交差は保留指値を取り消し、反対注文を出せます。約定ポジションには図独自の1%ストップと3%利確もあり、保護約定は残る注文参照を取り消します。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 確定足間隔。ギャラリーは5分、C#既定は4時間です。 |
| Fast EMA Length | 12 | 高速ExponentialMovingAverageに含める値の数。 |
| Slow EMA Length | 26 | 低速ExponentialMovingAverageに含める値の数。 |
| Momentum Length | 10 | 交差を符号で確認するMomentum値の数。 |

## ダイアグラムの詳細

- C#原典は成行で入り保留注文を管理しません。指値登録・置換・取消はここで示す意図的な執行変更です。
- 未約定指値はEMA順序、Momentum符号、Position側が有効な間だけ新しい終値へ移動します。
- 原典既定は4時間です。1か月H4ではEMA(26)形成がぎりぎりなため例は5分を使い、04:00:00へ戻せます。
- 基本数量1と1%ストップ・3%利確のPosition protectionは固定追加で、コンストラクタ引数ではありません。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
