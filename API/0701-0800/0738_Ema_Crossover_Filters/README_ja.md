# フィルター付きEMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、複数の指数移動平均線（EMA）を使用して、追加のトレンドフィルターを伴うクロスオーバーを取引します。

100 EMAが200 EMAを上抜けし、9 EMAが50 EMAを上回っているときに買いポジションを取ります。100 EMAが200 EMAを下抜けし、9 EMAが50 EMAを下回っているときにショートポジションを取ります。ロングポジションは100 EMAが50 EMAを下抜けしたときに決済され、ショートポジションは100 EMAが50 EMAを上抜けしたときに決済されます。

## パラメーター
- ローソク足の種類
- EMA 9 の期間
- EMA 50 の期間
- EMA 100 の期間
- EMA 200 の期間
