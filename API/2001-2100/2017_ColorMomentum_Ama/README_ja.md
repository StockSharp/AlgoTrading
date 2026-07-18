# Color Momentum AMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTraderエキスパートアドバイザー *Exp_ColorMomentum_AMA* をStockSharpに変換したものです。
設定可能な期間にわたって価格のモメンタムを計算し、カウフマン適応移動平均（AMA）で平滑化します。
平滑化されたモメンタムが2本連続で上昇または下降したときに取引シグナルが生成されます。

## ロジック
- **ロングエントリー**: Momentum AMAが2本連続で上昇。新しいロングポジションを開く前に既存のショートポジションを閉じます。
- **ショートエントリー**: Momentum AMAが2本連続で下降。新しいショートポジションを開く前に既存のロングポジションを閉じます。
- 逆シグナルが現在のポジションを閉じます。

## パラメーター
- ローソク足タイプ
- モメンタム期間
- AMA期間
- 高速期間
- 低速期間
- シグナルバー
