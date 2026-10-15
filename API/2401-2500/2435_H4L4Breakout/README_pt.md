# H4L4 Estratégia de Rompimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento diário que calcula os níveis H4 e L4 a partir do máximo, mínimo e fecho do dia anterior.
No início de cada dia é colocado um limite de venda em H4 e um limite de compra em L4.
Todas as posições abertas e ordens pendentes são canceladas antes de submeter novas ordens.
São aplicados stop loss e take profit de proteção usando distâncias baseadas em ticks.

## Detalhes

- **Critérios de entrada**: Limite de venda em H4 e limite de compra em L4 derivados da vela do dia anterior.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou take profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `TakeProfit` = 57
  - `StopLoss` = 7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
