# Modelo de Estratégia TradingViewTo com Alertas Dinâmicos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia modelo que abre posições com base em níveis de RSI e gerencia operações com stop loss e take profit percentuais.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI > `UpperLevel`
  - **Vendido**: RSI < `LowerLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop loss ou take profit
- **Stops**: Stop loss e take profit percentuais
- **Valores padrão**:
  - `RsiLength` = 14
  - `UpperLevel` = 60
  - `LowerLevel` = 40
  - `StopLossPct` = 2m
  - `TakeProfitPct` = 4m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
