# ゾーナルトレーディング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はAwesome Oscillator（AO）とAccelerator Oscillator（AC）を使用して、市場のモメンタムの変化を捉えます。

## ロジック
- AOとACの両方が前の値を上回り、かつ少なくともどちらかが前のバーから上向きに転換し、両オシレーターが正のときに買います。
- AOとACの両方が前の値を下回り、かつ少なくともどちらかが前のバーから下向きに転換し、両オシレーターが負のときに売ります。
- AOとACが下向きに転換したときにロングポジションを決済します。
- AOとACが上向きに転換したときにショートポジションを決済します。

## パラメーター
- **Candle Type** – 計算のソースとなるローソク足シリーズ。
- **Take Profit** – 価格単位での固定テイクプロフィット値。

この戦略は成行注文を使用して一度に一つのポジションのみを取引します。
