# VWAP Pro V21
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina EMA rápida e lenta com VWAP e gestão de risco baseada em ATR. Um filtro EMA de período superior (1h, comprimento 50) confirma a tendência. As operações abrem quando o preço se alinha com a tendência e fecham nos níveis de take profit ou stop loss baseados em ATR.

## Detalhes

- **Critérios de entrada**: Preço acima/abaixo da EMA rápida, VWAP e filtro de tendência.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Take profit ou stop loss baseado em ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaFastPeriod` = 9
  - `EmaSlowPeriod` = 21
  - `AtrPeriod` = 14
  - `TakeProfitAtrMultiplier` = 0.7
  - `StopLossAtrMultiplier` = 1.4
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, VWAP, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
