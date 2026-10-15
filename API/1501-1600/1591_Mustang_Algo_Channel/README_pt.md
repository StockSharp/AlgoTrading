# Estratégia de Canal Mustang Algo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza um oscilador de sentimento global baseado em RSI suavizado com WMA para operar cruzamentos de canal.

## Detalhes

- **Critérios de entrada**: Cruzamentos do oscilador RSI/WMA com os limites.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Sinal oposto ou stop/take.
- **Stops**: Percentuais, opcionais.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `Smoothing` = 20
  - `MedianPeriod` = 25
  - `UpperBound` = 55
  - `LowerBound` = 48
  - `TradeMode` = Long & Short
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `StopLossPercent` = 4
  - `TakeProfitPercent` = 12
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Configurável
  - Indicadores: RSI, WMA
  - Stops: Percentual
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
