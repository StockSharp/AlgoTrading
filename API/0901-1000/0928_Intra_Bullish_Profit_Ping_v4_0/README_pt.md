# Estratégia Intra Bullish - Profit Ping v4.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema somente comprado usando cruzamento de EMA confirmado pelo histograma MACD e força do RSI.

## Detalhes

- **Critérios de entrada**:
  - EMA curta cruza acima da EMA longa
  - Histograma MACD > 0
  - RSI > 50
  - Fechamento > Abertura
- **Critérios de saída**:
  - EMA curta cruza abaixo da EMA longa
  - Histograma MACD < 0
  - RSI < 50
  - Fechamento < Abertura
- **Indicadores**:
  - Médias Móveis Exponenciais
  - MACD
  - RSI
- **Stops**: Nenhum.
- **Valores padrão**:
  - `ShortEmaLength` = 7
  - `LongEmaLength` = 14
  - `RsiLength` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
- **Filtros**:
  - Seguidor de tendência
  - Período único
  - Indicadores: EMA, MACD, RSI
  - Stops: nenhum
  - Complexidade: Baixo
