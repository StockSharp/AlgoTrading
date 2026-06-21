# Estratégia Charles EMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia emula o consultor especialista Charles combinando médias móveis exponenciais (EMA) com um filtro RSI e um trailing stop. Opera em ambas as direções e protege posições dinamicamente.

O sistema monitora uma EMA rápida e uma lenta no período selecionado. Quando a EMA rápida cruza acima da EMA lenta e o RSI supera 55, a estratégia entra em uma posição comprada. De forma contrária, quando a EMA rápida cruza abaixo da EMA lenta e o RSI cai abaixo de 45, entra em uma posição vendida. Após a entrada, um trailing stop segue o preço para consolidar lucros enquanto um take profit fixo e stop loss são gerenciados pela proteção de posição integrada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `EMA rápida` cruza acima da `EMA lenta` e `RSI > 55`.
  - **Vendido**: `EMA rápida` cruza abaixo da `EMA lenta` e `RSI < 45`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Trailing stop.
  - Stop loss ou take profit.
- **Stops**: Usa proteção integrada com trailing.
- **Valores padrão**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 0.02
  - `StopLoss` = 0.008
  - `TrailStart` = 0.006
  - `TrailOffset` = 0.003
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 1 hora por padrão
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
