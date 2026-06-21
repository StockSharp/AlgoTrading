# Estratégia de Rompimentos de Linha de Tendência com Multi Fibonacci Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia faz a média de três cálculos de SuperTrend usando multiplicadores Fibonacci (0.618, 1.618, 2.618) e suaviza o resultado com uma EMA. Linhas de tendência dinâmicas são construídas a partir de máximas e mínimas de oscilação com inclinações derivadas do ATR. Uma operação comprada é aberta quando o preço rompe acima da linha de tendência superior, o SuperTrend suavizado está subindo e o valor +DI supera −DI. As operações vendidas espelham essas regras.

## Detalhes
- **Entrada**: rompimento de linha de tendência com confirmação do DMI e concordância do SuperTrend.
- **Saída**: preço cruzando de volta sobre a tendência suavizada ou atingindo o stop/alvo baseado em ATR‑.
- **Indicadores**: SuperTrend, ATR, Average Directional Index.
- **Tipo**: rompimento, comprado e vendido.
