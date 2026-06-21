# Estratégia Forex Fire EMA MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de tendência multi-período usando EMA, MA e confirmação do RSI. Usa velas de 4h para confluência e velas de 15m para entradas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: EMA curta acima da EMA longa, preço acima da MA, RSI rápido acima do RSI lento e >50, volume crescente com confirmação do período superior.
  - Vendido: Condições opostas.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento de EMA ou RSI atingindo limiares.
  - Stop loss, take profit, trailing stop e saída baseada em ATR opcionais.
- **Stops**: Sim, configurável.
- **Valores padrão**:
  - `EmaShortLength` = 13
  - `EmaLongLength` = 62
  - `MaLength` = 200
  - `MaType` = MovingAverageTypeEnum.Simple
  - `RsiSlowLength` = 28
  - `RsiFastLength` = 7
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
  - `UseTrailingStop` = true
  - `TrailingPercent` = 1.5
  - `UseAtrExits` = true
  - `AtrMultiplier` = 2
  - `AtrLength` = 14
  - `EntryCandleType` = TimeSpan.FromMinutes(15).TimeFrame()
  - `ConfluenceCandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, MA, RSI, ATR
  - Stops: Sim
  - Complexidade: Médio
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
