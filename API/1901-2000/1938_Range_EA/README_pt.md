# Estratégia Range EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera desvios de preço a partir de uma média móvel dentro de um intervalo fixo. Abre posições compradas ou vendidas quando o preço se move uma distância especificada da média. Suporta trailing stop opcional, média escalonada, módulo de reversão e filtro de sessão de trading.

## Detalhes

- **Critérios de entrada**:
  - Comprado: preço de fechamento acima da média móvel + `Range`
  - Vendido: preço de fechamento abaixo da média móvel - `Range`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Atingir `TakeProfit` ou `StopLoss`
  - Trailing stop ativado quando habilitado
  - Reversão opcional após movimento de `Turn`
- **Stops**: Valor fixo
- **Valores padrão**:
  - `MaLength` = 21
  - `Range` = 250m
  - `TakeProfit` = 500m
  - `StopLoss` = 250m
  - `UseTrailingStop` = true
  - `TrailingStop` = 250m
  - `UseTurn` = true
  - `Turn` = 250m
  - `LotMultiplicator` = 1.65m
  - `TurnTakeProfit` = 500m
  - `UseStepDown` = false
  - `StepDown` = 150m
  - `UseTradeTime` = false
  - `OpenTradeTime` = 08:00:00
  - `CloseTradeTime` = 21:30:00
  - `OrderVolume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
