# Estratégia de Taxa de Variação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa o indicador de Taxa de Variação para detectar condições de bolha e negociar cruzamentos da linha zero com dimensionamento dinâmico de posição.

Os backtests mostram desempenho estável em dados diários para ativos principais.

## Detalhes

- **Critérios de entrada**: ROC cruza acima ou abaixo de zero; vendido opcional ao estouro da bolha.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `RocLength` = 365
  - `BubbleThreshold` = 180m
  - `StopLossPercent` = 6m
  - `FixedRatioValue` = 400m
  - `IncreasingOrderAmount` = 200m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RateOfChange
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
