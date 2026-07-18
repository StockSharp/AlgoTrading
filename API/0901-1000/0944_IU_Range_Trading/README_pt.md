# Estratégia IU de Negociação em Intervalo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia IU Range Trading identifica zonas de consolidação onde o intervalo de preços durante um período de lookback permanece dentro de um multiplicador ATR. Operações de rompimento são acionadas quando o preço excede os limites do intervalo. As posições são protegidas por um stop trailing baseado em ATR que se move com a ação favorável do preço.

## Detalhes

- **Critérios de entrada**: O preço rompe acima ou abaixo de um intervalo estreito definido por ATR.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop trailing baseado em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `RangeLength` = 10
  - `AtrLength` = 14
  - `AtrTargetFactor` = 2.0m
  - `AtrRangeFactor` = 1.75m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
