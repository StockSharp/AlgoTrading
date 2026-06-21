# Estratégia Asimmetric Stoch NR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em linhas assimétricas do oscilador estocástico. A estratégia reage aos cruzamentos de %K e %D e suporta proteção opcional de posição.

O método alterna períodos para o cálculo de %K para se adaptar ao ruído do mercado. Stop-loss e take-profit são aplicados em unidades de preço absolutas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `%K` cruza acima de `%D`
  - Vendido: `%K` cruza abaixo de `%D`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `%K` cruza abaixo de `%D`
  - Vendido: `%K` cruza acima de `%D`
- **Stops**: absolutos em `StopLoss` e `TakeProfit`
- **Valores padrão**:
  - `KPeriodShort` = 5
  - `KPeriodLong` = 12
  - `DPeriod` = 7
  - `Slowing` = 3
  - `Overbought` = 80m
  - `Oversold` = 20m
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
