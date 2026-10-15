# CANX MAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は中間価格（HL2）のEMAクロスオーバーを取引します。高速EMAが低速EMAを上抜けた場合にロングポジションを建てます。ロングのみモードが無効の場合、高速EMAが低速EMAを下抜けた場合にショートポジションを建てます。開始年フィルターにより、指定した年より前の取引を防ぎます。

## パラメーター
- ローソク足タイプ
- 高速EMAの長さ
- 乗数（低速EMA = 高速の長さ × 乗数）
- ロングのみ
- 開始年
