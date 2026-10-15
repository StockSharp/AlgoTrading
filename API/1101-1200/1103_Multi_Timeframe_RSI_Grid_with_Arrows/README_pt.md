# Estratégia de Grade RSI Multitemporal com Setas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera quando o RSI no período atual e em dois períodos superiores atingem níveis de sobrecompra ou sobrevenda. A primeira posição é aberta quando todos os RSIs estão alinhados, então posições adicionais são adicionadas usando uma grade baseada em ATR com um multiplicador de lote crescente. A estratégia mira um percentual de lucro diário, reinicia a cada dia e fecha em sinais inversos ou por drawdown.

## Parâmetros
- Tipo de vela
- Comprimento do RSI
- Nível de sobrevenda
- Nível de sobrecompra
- Período superior 1
- Período superior 2
- Fator de multiplicação da grade
- Fator de multiplicação de lote
- Níveis máximos de grade
- Meta de lucro diário %
- Comprimento do ATR
