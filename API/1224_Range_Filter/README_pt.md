# Estratégia de Filtro de Intervalo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de filtro de intervalo com cálculo de intervalo realista e níveis fixos de risco/recompensa.

Utiliza um intervalo suavizado para criar bandas dinâmicas ao redor do preço. As operações são realizadas quando o preço rompe acima ou abaixo dessas bandas. O gerenciamento de risco usa distâncias fixas de stop loss e take profit.

## Detalhes

- **Critérios de entrada**: O preço rompe as bandas do filtro de intervalo.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou take profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `SamplingPeriod` = 100
  - `RangeMultiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Range filter
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
