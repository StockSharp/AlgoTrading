# Bear Bulls Power戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTrader 5エキスパート「Exp_Bear_Bulls_Power」の変換版です。平滑化されたBear/Bulls Powerインジケーターを使用してトレンド転換を検出します。

## 動作原理

1. 各ローソク足の中値価格を計算する: `(High + Low) / 2`。
2. 長さ`FirstLength`の移動平均で中値価格を平滑化する。
3. 中値価格とその移動平均の差を計算する。
4. 長さ`SecondLength`の移動平均で2回目の平滑化を適用する。
5. 現在の平滑化値と前の値を比較してトレンド方向を決定する。
6. 方向が変わるときにシグナルを生成する:
   - ゼロより上での上向きの転換がロングポジションを開く。
   - ゼロより下での下向きの転換がショートポジションを開く。

## パラメーター

- **Candle Type** – 処理するローソク足の時間軸。
- **First Length** – 価格平滑化の期間。
- **Second Length** – シグナル平滑化の期間。

戦略は成行注文を使用し、完成したローソク足のみで機能します。
