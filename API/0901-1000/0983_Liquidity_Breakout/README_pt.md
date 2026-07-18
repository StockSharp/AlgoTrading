# Estratégia de Rompimento de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de uma faixa de preços recente definida por máximas e mínimas pivot. Uma posição é aberta quando o preço fecha além dos extremos da faixa anterior. O stop loss opcional pode usar uma linha SuperTrend ou uma porcentagem fixa.

## Detalhes

- **Critérios de entrada**:
  - `Fechamento > máxima da faixa anterior` → comprado
  - `Fechamento < mínima da faixa anterior` → vendido
- **Comprado/Vendido**: Configurável (Comprado, Vendido, Ambos).
- **Critérios de saída**: Rompimento oposto ou stop loss.
- **Stops**: SuperTrend ou porcentagem fixa.
- **Valores padrão**:
  - `PivotLength` = 12
  - `StopLoss` = SuperTrend
  - `FixedPercentage` = 0.1
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Highest, Lowest, SuperTrend
  - Stops: Opcional
  - Complexidade: Baixo
  - Período: 1h
