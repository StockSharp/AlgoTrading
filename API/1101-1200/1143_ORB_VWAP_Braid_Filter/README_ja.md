# ORB VWAP Braid Filter 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VWAPとBraidフィルターの確認を使用したオープニングレンジブレイクアウト戦略。

## ルール
- 取引所時間09:35から11:00の間に取引
- 1日1取引
- 価格がオープニングレンジ高値を上回り、VWAPの上にあり、Braidフィルターが強気の場合にロング
- 価格がオープニングレンジ安値を下回り、VWAPの下にあり、Braidフィルターが弱気の場合にショート
- ストップロスはレンジの反対側
- テイクプロフィットはリスクの2倍、前日またはプレマーケットの水準に制限

## インジケーター
- 出来高加重移動平均 (VWAP)
- 指数移動平均 (3, 7, 14)
- Average True Range (14)
