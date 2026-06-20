# Estratégia de Pullback Fibonacci Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia calcula a média de três linhas SuperTrend construídas com multiplicadores de Fibonacci (0.618, 1.618, 2.618) e suaviza o resultado com uma EMA. As operações seguem pullbacks para essa tendência adaptativa, enquanto uma linha média baseada em AMA e um filtro RSI opcional confirmam a direção.

## Detalhes

- **Critérios de entrada**:
  - Mínimo abaixo do SuperTrend médio e fechamento acima do seu valor suavizado.
  - O fechamento anterior em relação à linha média AMA define o pullback.
  - **Comprado**: fechamento acima da linha média e RSI > limiar.
  - **Vendido**: fechamento abaixo da linha média e RSI < limiar.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Fechamento cruzando o SuperTrend suavizado na direção oposta.
- **Stops**: Stop loss e take profit percentuais via `StartProtection`.
- **Valores padrão**:
  - `AtrPeriod` = 8
  - `SmoothLength` = 21
  - `AmaLength` = 55
  - `RsiLength` = 7
  - `RsiBuy` = 70
  - `RsiSell` = 30
  - `TakeProfitPercent` = 5
  - `StopLossPercent` = 0.75
- **Filtros**:
  - Categoria: Pullback de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, EMA, AMA, RSI
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
