# Estratégia Supertrend Multi-Passo Estratégica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia utiliza dois cálculos de Supertrend para detectar entradas e saídas com alvos de lucro de múltiplos passos configuráveis.

## Detalhes

- **Critérios de entrada**: Sinais baseados nas direções de dois Supertrend.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Supertrend oposto ou níveis de take profit.
- **Stops**: Passos de take profit.
- **Valores padrão**:
  - `UseTakeProfit` = true
  - `TakeProfitPercent1` = 6.0
  - `TakeProfitPercent2` = 12.0
  - `TakeProfitPercent3` = 18.0
  - `TakeProfitPercent4` = 50.0
  - `TakeProfitAmount1` = 12
  - `TakeProfitAmount2` = 8
  - `TakeProfitAmount3` = 4
  - `TakeProfitAmount4` = 0
  - `NumberOfSteps` = 3
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 5
  - `Factor2` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Configurável
  - Indicadores: ATR, Supertrend
  - Stops: Take profit
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
