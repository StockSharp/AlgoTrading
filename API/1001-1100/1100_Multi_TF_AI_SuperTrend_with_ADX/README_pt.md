# Estratégia Multi-TF AI SuperTrend com ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina dois indicadores SuperTrend filtrados por uma verificação de força ADX. A direção da tendência é confirmada comparando as WMAs do preço com as WMAs do SuperTrend. Operações compradas são abertas quando ambos os SuperTrends estão em alta e o ADX mostra força positiva. Operações vendidas são abertas nas condições opostas. O ATR do primeiro SuperTrend fornece um stop trailing.

- **Comprado**: Ambos os SuperTrends em alta, WMAs do preço acima das WMAs do SuperTrend, +DI > -DI e ADX acima do limiar.
- **Vendido**: Ambos os SuperTrends em baixa, WMAs do preço abaixo das WMAs do SuperTrend, -DI > +DI e ADX acima do limiar.
- **Indicadores**: SuperTrend, WMA, ATR, ADX.
- **Stops**: Stop trailing baseado no ATR do primeiro SuperTrend.
