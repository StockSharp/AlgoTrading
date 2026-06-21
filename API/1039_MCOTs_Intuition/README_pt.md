# Estratégia MCOTs Intuition
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no momentum do RSI relativo ao seu desvio padrão. Compra quando o momentum ascendente é forte mas está diminuindo e vende nas condições opostas. Alvos de lucro fixos e stop loss são colocados em ticks.

## Detalhes

- **Critérios de entrada**:
  - Comprado: momentum > stdDev * multiplier e momentum < previousMomentum * exhaustionMultiplier
  - Vendido: momentum < -stdDev * multiplier e momentum > previousMomentum * exhaustionMultiplier
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Alvo de lucro fixo e stop loss em ticks
- **Stops**: Sim
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `StdDevMultiplier` = 1m
  - `ExhaustionMultiplier` = 1m
  - `ProfitTargetTicks` = 40
  - `StopLossTicks` = 160
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI, StandardDeviation
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
