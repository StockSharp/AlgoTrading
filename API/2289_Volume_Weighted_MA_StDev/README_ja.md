# 出来高加重MA標準偏差戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は標準偏差フィルターを持つ出来高加重移動平均（VWMA）を適用します。VWMAのモメンタムを測定し、上昇方向の動きが設定可能な偏差閾値を超えるとロングポジションを開きます。下降方向の動きが負の閾値を越えるとショートポジションを開きます。このアプローチは出来高で確認された強い方向性のある動きを捉えることを試みます。

## パラメーター
- 足の種類
- VWMAの長さ
- StdDevの期間
- K1
- K2
