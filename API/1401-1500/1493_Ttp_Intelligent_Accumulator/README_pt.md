# Acumulador Inteligente Ttp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que acumula posições compradas quando o RSI cai abaixo de sua média por um desvio padrão e as distribui quando o RSI sobe acima do mesmo limiar.

## Detalhes

- **Critérios de entrada**: RSI < SMA(RSI, `MaPeriod`) - StdDev(RSI, `StdPeriod`)
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: RSI > SMA(RSI, `MaPeriod`) + StdDev(RSI, `StdPeriod`) e lucro acima de `MinProfit`
- **Stops**: Não
- **Valores padrão**:
  - `RsiPeriod` = 7
  - `MaPeriod` = 14
  - `StdPeriod` = 14
  - `AddWhileInLossOnly` = true
  - `MinProfit` = 0m
  - `ExitPercent` = 100m
  - `UseDateFilter` = false
  - `StartDate` = 2022-06-01
  - `EndDate` = 2030-07-01
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: RSI, MA, StdDev
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
