# Estratégia Turtle Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Turtle Trader segue o clássico sistema de rompimento Turtle usando canais Donchian e gestão de capital baseada em ATR. Compra quando o preço supera máximas recentes e vende quando cai abaixo de mínimas recentes. O piramidamento adiciona às posições vencedoras conforme o preço se move a favor.

## Detalhes

- **Critérios de entrada**: rompimento de máximas/mínimas de `S1` ou `S2`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: rompimento oposto ou stop ATR
- **Stops**: baseados em ATR
- **Valores padrão**:
  - `RiskPercent` = 1
  - `AtrPeriod` = 20
  - `StopMultiplier` = 1.5
  - `PyramidProfit` = 0.5
  - `S1Long` = 20
  - `S2Long` = 55
  - `S1LongExit` = 10
  - `S2LongExit` = 20
  - `S1Short` = 15
  - `S2Short` = 55
  - `S1ShortExit` = 7
  - `S2ShortExit` = 20
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: ATR
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
