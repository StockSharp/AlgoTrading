# Estratégia de RSI Simples para Ações 1D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema entra comprado quando o RSI cai abaixo de um nível de sobrevenda enquanto o preço permanece acima da SMA de 200 dias. A posição usa um stop baseado em ATR e três alvos de lucro.

## Detalhes

- **Critérios de entrada**: RSI abaixo de `OversoldLevel` e fechamento acima do filtro SMA.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop ATR ou atingimento de qualquer nível de take profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 5
  - `OversoldLevel` = 30
  - `SmaLength` = 200
  - `AtrLength` = 20
  - `AtrMultiplier` = 1.5
  - `TakeProfit1` = 5
  - `TakeProfit2` = 10
  - `TakeProfit3` = 15
  - `StopLossPercent` = 25
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Comprado
  - Indicadores: RSI, SMA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
