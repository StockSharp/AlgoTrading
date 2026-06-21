# Merovinh - 最安値による平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、振り返り期間内の現在の最安値が以前の安値を設定回数連続して更新したときに買いエントリーします。同じ期間内に新しい最高値が現れたときにポジションを決済します。

## パラメーター
- Bars — 最高値/最安値の振り返り期間の長さ。
- Number Of Lows — エントリーに必要な連続した安値更新回数。
- Start Date / End Date — 取引期間。
- Candle Type — ローソク足の種類。
