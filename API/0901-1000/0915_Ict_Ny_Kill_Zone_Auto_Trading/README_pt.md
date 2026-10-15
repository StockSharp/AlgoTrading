# Estratégia de Trading Automático ICT NY Kill Zone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera durante a kill zone de Nova York utilizando fair value gaps e order blocks.

## Detalhes

- **Critérios de entrada**: Fair value gap e order block dentro da kill zone.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Proteção de posição.
- **Stops**: Sim.
- **Valores padrão**:
  - `StopLoss` = 30
  - `TakeProfit` = 60
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

