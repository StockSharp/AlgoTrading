# Estratégia de Rompimento de Ação de Preço Pura com RR 1:5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento de Ação de Preço Pura com RR 1:5 utiliza o cruzamento de duas EMAs confirmado por RSI e volume. O stop loss é baseado no ATR e o take profit é cinco vezes o risco.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida cruza acima da EMA lenta, RSI > 50, volume acima da SMA de 20 períodos.
  - **Vendido**: EMA rápida cruza abaixo da EMA lenta, RSI < 50, volume acima da SMA de 20 períodos.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop loss baseado em ATR e take profit com risco-recompensa 1:5.
- **Stops**: Stop loss = 1.5 × ATR, take profit = 5 × risco.
- **Valores padrão**:
  - `FastPeriod` = 9
  - `SlowPeriod` = 21
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `VolumePeriod` = 20
  - `StopLossFactor` = 1.5
  - `RiskRewardRatio` = 5
  - `MaxTradesPerDay` = 5
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: EMA, RSI, ATR, Volume SMA
  - Stops: Stop loss ATR, take profit 1:5
  - Complexidade: Baixo
  - Período: 5m ou 15m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
