# Estratégia de Variação Color J
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que replica o Expert Advisor ColorJVariation utilizando a Média Móvel Jurik. Rastreia a inclinação da JMA e entra quando a direção muda. A estratégia suporta níveis absolutos de stop loss e take profit.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `PrevSlopeDown && JMA turns up`
  - Vendido: `PrevSlopeUp && JMA turns down`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal de reversão oposto
- **Stops**: Absolutos via `StopLoss` e `TakeProfit`
- **Valores padrão**:
  - `JmaPeriod` = 12
  - `JmaPhase` = 100
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoria: Reversão de tendência
  - Direção: Ambos
  - Indicadores: Jurik Moving Average
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
