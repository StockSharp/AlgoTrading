# Seguidor de Tendência PercentX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia derivada do PercentX Trend Follower de Trendoscope.

A estratégia normaliza a distância do preço em relação a uma banda selecionada (Keltner ou Bollinger) e opera quando este oscilador cruza faixas extremas dinâmicas. O ATR é usado para o posicionamento dos stops.

## Detalhes

- **Critérios de entrada**: O oscilador cruza acima da faixa superior para comprado, abaixo da faixa inferior para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop baseado em ATR.
- **Stops**: Stop inicial por ATR.
- **Valores padrão**:
  - `BandType` = Keltner
  - `MaLength` = 40
  - `LoopbackPeriod` = 80
  - `OuterLoopback` = 80
  - `UseInitialStop` = true
  - `AtrLength` = 14
  - `TrendMultiplier` = 1
  - `ReverseMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: BollingerBands, KeltnerChannels, ATR, Highest, Lowest
  - Stops: ATR
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
