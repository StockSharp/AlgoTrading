# Estratégia de Trading com Duplo AI SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa dois indicadores SuperTrend combinados com médias móveis ponderadas para confirmar a direção da tendência. Operações compradas são abertas quando ambos os SuperTrends são altistas e as WMAs de preço permanecem acima das WMAs de SuperTrend correspondentes. Operações vendidas ocorrem nas condições opostas. As posições são gerenciadas com um stop trailing baseado em ATR do primeiro SuperTrend.

- **Comprado**: Ambos os SuperTrends altistas e WMAs de preço acima das WMAs de SuperTrend.
- **Vendido**: Ambos os SuperTrends baixistas e WMAs de preço abaixo das WMAs de SuperTrend.
- **Indicadores**: SuperTrend, WMA, ATR.
- **Stops**: Stop trailing baseado no primeiro SuperTrend.
