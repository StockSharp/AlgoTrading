# SpeedBullish Strategy Confirm V6.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
EMAトレンドフィルター、MACDヒストグラムクロスオーバー、RSI閾値を組み合わせた戦略です。オプションのATRと出来高フィルターでシグナルの質を向上させます。

## エントリー条件
- ロングはEMA10またはEMA15を上回る価格、ショートは下回る価格。
- ロングはMACDヒストグラムがゼロを上抜け、ショートはゼロを下抜け。
- RSIが指定レベルより高いまたは低い。
- オプション：ATRがその移動平均をマルチプライヤー倍上回ること。
- オプション：出来高がSMAをマルチプライヤー倍上回ること。

## エグジット条件
- 逆のエントリーシグナル。
- ポイント単位のテイクプロフィットとトレーリングストップ。
- ポイント単位の手動ストップロス。
