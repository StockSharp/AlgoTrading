# Estratégia Diária Supertrend EMA Crossover com Filtro RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera cruzamentos de EMA somente quando o Supertrend confirma a direção e o RSI é favorável. Usa níveis de stop loss e take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Fast EMA` cruza acima de `Slow EMA`, Supertrend em tendência de alta, `RSI < RsiOverbought`
  - Vendido: `Fast EMA` cruza abaixo de `Slow EMA`, Supertrend em tendência de baixa, `RSI > RsiOversold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss ou take profit baseado em ATR
- **Stops**: Sim
- **Valores padrão**:
  - `FastEmaLength` = 3
  - `SlowEmaLength` = 6
  - `AtrLength` = 3
  - `StopLossMultiplier` = 2.5m
  - `TakeProfitMultiplier` = 4m
  - `RsiLength` = 10
  - `RsiOverbought` = 65m
  - `RsiOversold` = 30m
  - `SupertrendMultiplier` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, Supertrend, RSI, ATR
  - Stops: Múltiplos de ATR
  - Complexidade: Intermediário
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
