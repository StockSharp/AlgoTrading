# Estratégia Hammer + EMA com SL/TP baseado em ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina os padrões de velas Hammer e Hammer invertido com um filtro de tendência EMA e gestão de risco baseada em ticks.

## Detalhes

- **Critérios de entrada**: Hammer acima da EMA ou Hammer invertido abaixo da EMA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Take profit ou stop loss baseado em ticks.
- **Stops**: Baseado em ticks.
- **Valores padrão**:
  - `EmaLength` = 50
  - `StopLossTicks` = 1
  - `TakeProfitTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: EMA, Hammer, Hammer invertido
  - Stops: Baseado em ticks
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
