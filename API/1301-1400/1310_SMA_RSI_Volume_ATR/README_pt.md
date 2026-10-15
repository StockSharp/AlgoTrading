# Estratégia SMA RSI Volume ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma Média Móvel Simples (SMA), o Índice de Força Relativa (RSI), confirmação de volume e um filtro de volatilidade baseado em ATR.
Compra quando o preço está acima da SMA, o RSI está sobrevendido, o volume supera sua média móvel por um multiplicador e a volatilidade está aumentando. Vende sob as condições opostas.

Os stops são gerenciados com níveis fixos de take profit e stop loss em percentual.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close > SMA` && `RSI < RsiOversold` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
  - **Vendido**: `Close < SMA` && `RSI > RsiOverbought` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou take-profit
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `SmaLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `VolumeThreshold` = 1.5
  - `AtrLength` = 14
  - `TakeProfitPerc` = 1.5
  - `StopLossPerc` = 0.5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, RSI, Volume, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
