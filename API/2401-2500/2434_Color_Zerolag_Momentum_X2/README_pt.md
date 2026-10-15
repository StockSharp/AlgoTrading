# Estratégia Color Zerolag Momentum X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de momentum em duplo período temporal usando um cruzamento de média móvel de zero lag. O período temporal superior define a direção da tendência, enquanto o período temporal inferior aciona entradas quando o momentum cruza a sua média de zero lag na direção da tendência.

## Detalhes

- **Critérios de entrada**: o momentum cruza a sua média de zero lag na direção da tendência
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: cruzamento oposto ou reversão de tendência
- **Stops**: Não
- **Valores padrão**:
  - `TrendCandleType` = 6h
  - `TrendMomentumPeriod` = 34
  - `TrendMaLength` = 15
  - `SignalCandleType` = 30m
  - `SignalMomentumPeriod` = 34
  - `SignalMaLength` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Momentum, ZeroLagEMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
