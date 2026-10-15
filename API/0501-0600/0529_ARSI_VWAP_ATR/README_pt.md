# Estratégia Arsi Vwap Atr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de RSI adaptativo onde os níveis de sobrecompra e sobrevenda expandem ou contraem com base no ATR ou no desvio do VWAP. As posições são abertas em cruzamentos do RSI sobre os níveis adaptativos e fechadas quando o RSI retorna à zona intermediária.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `RSI` cruza acima da linha adaptativa de sobrevenda
  - Vendido: `RSI` cruza abaixo da linha adaptativa de sobrecompra
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - RSI cruza de volta por 50 ou a linha adaptativa oposta
- **Stops**: Baseado em percentual usando `StopLossPercent` e `RiskReward`
- **Valores padrão**:
  - `RsiLength` = 14
  - `BaseK` = 1m
  - `RiskPercent` = 2m
  - `StopLossPercent` = 2.5m
  - `RiskReward` = 2m
  - `SourceOb` = ATR
  - `SourceOs` = ATR
  - `AtrLengthOb` = 14
  - `AtrLengthOs` = 14
  - `ObMultiplier` = 10m
  - `OsMultiplier` = 10m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI, ATR, VWAP
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
