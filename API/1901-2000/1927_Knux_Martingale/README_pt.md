# Estratégia Knux Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de Martingale que aumenta o volume de operação após uma posição perdedora. O método filtra as entradas pelo Average Directional Index (ADX) para operar apenas em mercados com tendência. Candles de alta abrem posições compradas, candles de baixa abrem posições vendidas.

## Detalhes

- **Critérios de entrada**:
  - ADX > 25
  - Comprado: `Close > Open`
  - Vendido: `Close < Open`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss ou take profit
- **Stops**: Sim
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `LotsMultiplier` = 1.5m
  - `StopLoss` = 150m
  - `TakeProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência, Martingale
  - Direção: Ambos
  - Indicadores: AverageDirectionalIndex
  - Stops: Absoluto
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
