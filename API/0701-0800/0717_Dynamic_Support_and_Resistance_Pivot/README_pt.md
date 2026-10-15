# Estratégia Dinâmica de Pivô de Suporte e Resistência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia deriva níveis dinâmicos de suporte e resistência a partir de máximas e mínimas de pivô recentes. Entra comprado quando o preço cruza acima do suporte próximo ao nível e entra vendido quando o preço cruza abaixo da resistência. O gerenciamento de risco usa níveis fixos de stop-loss e take-profit em percentual.

## Detalhes

- **Critérios de entrada**: Preço próximo ao suporte/resistência dentro do percentual `SupportResistanceDistance` e cruzamento acima do suporte ou abaixo da resistência.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take-profit e stop-loss fixos.
- **Stops**: Sim.
- **Valores padrão**:
  - `PivotLength` = 2
  - `SupportResistanceDistance` = 0.4m
  - `StopLossPercent` = 10.0m
  - `TakeProfitPercent` = 26.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Pivot
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
