# RMACDリバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はMoving Average Convergence Divergence (MACD)インジケーターを使用してリバーサルシグナルを生成します。4つの異なるモードがエントリーの検出方法を定義します：

1. **Breakdown** – MACDヒストグラムがゼロを下抜けるとロングエントリー、上抜けるとショートエントリー。
2. **MacdTwist** – 最後の2つのヒストグラム値を比較してMACD方向の変化を探します。
3. **SignalTwist** – シグナルラインの方向変化を監視します。
4. **MacdDisposition** – MACDヒストグラムがシグナルラインを交差するとエントリー。

戦略は常に成行注文を使用し、新しい反対シグナルが現れるとポジションをリバースします。

## パラメーター
- **Fast Length** – MACD内の速いEMAのピリオド。
- **Slow Length** – MACD内の遅いEMAのピリオド。
- **Signal Length** – シグナルラインの平滑化ピリオド。
- **Candle Type** – 計算に使用するローソク足の時間軸。
- **Mode** – 上記で説明したエントリーアルゴリズムを選択。

## 注意事項
- シグナルは完成したローソク足のみで評価されます。
- 戦略はヒストリカルデータを要求するのではなく、以前のMACD値を内部で保存します。
- 明示的なストップロスやテイクプロフィットは使用されません。ポジションは反対シグナルのみでクローズされます。
