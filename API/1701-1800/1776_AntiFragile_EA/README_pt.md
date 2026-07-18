# Estratégia AntiFragile EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de grade que coloca ordens limitadas em camadas acima e abaixo do preço atual com volume crescente.
As posições são protegidas por um stop inicial e acompanhadas com trailing stop à medida que o preço se move favoravelmente.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Colocar ordens buy limit a cada `SpaceBetweenTrades` passos abaixo do bid.
  - Vendido: Colocar ordens sell limit a cada `SpaceBetweenTrades` passos acima do ask.
- **Comprado/Vendido**: Opcional para cada lado via `TradeLong` e `TradeShort`.
- **Critérios de saída**: Trailing stop ou execução do lado oposto da grade.
- **Stops**: `StopLossPips` inicial e trailing via `TrailingStopPips`.
- **Valores padrão**:
  - `StartingVolume` = 0.1m
  - `IncreasePercentage` = 1m
  - `SpaceBetweenTrades` = 700m
  - `NumberOfTrades` = 50
  - `StopLossPips` = 300m
  - `TrailingStopPips` = 100m
  - `TradeLong` = true
  - `TradeShort` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Trading em grade
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
