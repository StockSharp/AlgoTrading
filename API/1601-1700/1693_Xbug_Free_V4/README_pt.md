# Estratégia Xbug Free V4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre posições quando uma média móvel do preço mediano cruza o próprio preço mediano. Um take profit e um stop loss simétricos são colocados a uma distância fixa do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - Comprado: a média móvel está acima do preço mediano e estava abaixo dele dois candles atrás
  - Vendido: a média móvel está abaixo do preço mediano e estava acima dele dois candles atrás
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Take profit a distância `StopPoints` acima/abaixo da entrada
  - Stop loss a distância `StopPoints` no lado oposto
- **Stops**: Sim
- **Valores padrão**:
  - `MaPeriod` = 19
  - `StopPoints` = 270
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Crossover
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
