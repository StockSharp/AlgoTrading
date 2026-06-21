# Estratégia Puria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Puria é uma estratégia seguidora de tendência que combina uma EMA rápida, duas LWMA lentas do preço mínimo e um filtro MACD. Uma posição comprada é aberta quando a EMA de 5 períodos está acima de ambas as LWMA de 75 e 85 períodos, o fechamento anterior está acima da EMA e a linha MACD é positiva. Uma posição vendida é aberta quando as condições opostas são satisfeitas. A estratégia usa níveis fixos de take-profit e stop-loss e permite apenas uma posição por direção até que um sinal oposto apareça.

## Detalhes
- **Critérios de entrada**: EMA(5) acima de LWMA(75) e LWMA(85), fechamento anterior acima da EMA, MACD(15,26) > 0 para comprados; inverso para vendidos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Distâncias fixas de stop-loss e take-profit em pontos de preço.
- **Valores padrão**:
  - `StopLoss` = 14
  - `TakeProfit` = 15
  - `Ma1Period` = 75
  - `Ma2Period` = 85
  - `Ma3Period` = 5
  - `CandleType` = Período de 1 minuto
- **Filtros**: Filtro de linha zero do MACD.
