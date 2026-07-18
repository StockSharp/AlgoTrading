# RSI 買い/売り圧力戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は受信したローソク足で RSI を計算し、EMA でスムージングします。
買い圧力と売り圧力を表す 2 つのライン `cc` と `bb` を導出します。
`cc` が `bb` を上抜けするとロングポジションを開き、`cc` が `bb` を下抜けするとショートポジションを開きます。
