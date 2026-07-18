# MACD Multitemporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O MACD Multitemporal combina sinais MACD do período de trabalho e de um período superior. As entradas ocorrem quando ambos os períodos concordam usando cruzamentos de linha ou cruzamentos da linha zero.

## Detalhes
- **Dados**: Velas de preço de dois períodos.
- **Critérios de entrada**:
  - **Comprado**: Depende do parâmetro `Entry`. Por padrão, cruzamento altista em ambos os períodos.
  - **Vendido**: Oposto ao comprado.
- **Critérios de saída**: Sinal oposto ou stop trailing.
- **Stops**: Stop trailing opcional.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = tf(5)
  - `HigherCandleType` = tf(1d)
  - `ShowCurrentTimeframe` = true
  - `ShowHigherTimeframe` = true
  - `Entry` = Crossover
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 2
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado e Vendido
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Multitemporal (5m/1d)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
