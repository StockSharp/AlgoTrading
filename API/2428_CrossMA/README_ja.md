# CrossMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はATRベースのストップロスを使用したシンプルな移動平均クロスオーバーを取引します。高速SMAが低速SMAを上方クロスするとロングポジションが開かれます。高速SMAが低速SMAを下方クロスするとショートポジションが開かれます。ポジションに入った後、エントリー価格から1 ATR離れた位置にストップロスが置かれ、新しいローソク足ごとに確認されます。

## パラメーター
- ローソク足タイプ
- 高速SMAの期間
- 低速SMAの期間
- ATRの期間
- ボリューム
