# Estratégia Mutanabby AI Algo Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Mutanabby AI Algo Pro entra comprada quando um padrão de vela envolvente altista se alinha com uma leitura de RSI abaixo de um limiar e uma queda de preço durante um número especificado de barras. As saídas ocorrem em um padrão envolvente baixista ou quando o stop loss é atingido.

## Detalhes
- **Critérios de entrada**: Envolvente altista, vela estável, RSI abaixo do limiar, preço abaixo do valor de N barras atrás.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Envolvente baixista ou stop loss.
- **Stops**: Opcional.
- **Valores padrão**:
  - `CandleStabilityIndex` = 0.5
  - `RsiIndex` = 50
  - `CandleDeltaLength` = 5
  - `DisableRepeatingSignals` = false
  - `EnableStopLoss` = true
  - `StopLossMethod` = EntryPriceBased
  - `EntryStopLossPercent` = 2.0
  - `LookbackPeriod` = 10
  - `StopLossBufferPercent` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
