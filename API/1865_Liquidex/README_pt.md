# Estratégia Liquidex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento que entra quando o preço se move além das bandas do Canal de Keltner e gerencia o risco com stop loss, take profit, break-even e trailing stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado: fechamento acima da banda superior do Canal de Keltner.
  - Vendido: fechamento abaixo da banda inferior do Canal de Keltner.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Nível de stop loss ou take profit atingido.
  - Stop movido para break-even após atingir o alvo de lucro.
  - Trailing stop ativado.
- **Stops**: Sim.
- **Valores padrão**:
  - `KcPeriod` = 10
  - `UseKcFilter` = true
  - `StopLoss` = 30
  - `TakeProfit` = 0
  - `MoveToBe` = 15
  - `MoveToBeOffset` = 2
  - `TrailingDistance` = 5
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Canal
  - Direção: Ambos
  - Indicadores: Keltner
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
