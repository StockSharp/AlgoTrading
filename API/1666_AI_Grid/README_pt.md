# Estratégia de Grade AI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Grade AI coloca ordens de compra e venda em camadas ao redor do preço atual. A estratégia suporta abordagens de rompimento (stop) e contratendência (limite). Após uma ordem ser executada, uma ordem de take-profit é colocada automaticamente.

## Detalhes

- **Critérios de entrada**: O preço alcança um dos níveis da grade.
- **Comprado/Vendido**: Controlado via `AllowLong` e `AllowShort`.
- **Critérios de saída**: Take-profit após distância fixa `TakeProfit`.
- **Stops**: Sem stop-loss.
- **Valores padrão**:
  - `GridSize` = 50m
  - `GridSteps` = 10
  - `TakeProfit` = 50m
  - `AllowLong` = true
  - `AllowShort` = true
  - `UseBreakout` = true
  - `UseCounter` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Grid
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Apenas take-profit
  - Complexidade: Intermediário
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
