# Estratégia Vegas SuperTrend Enhanced presentTrading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina um canal Vegas com um SuperTrend ajustado.
Entra quando o SuperTrend muda de direção com multiplicador baseado em volatilidade.

## Detalhes

- **Critérios de entrada**: mudanças de tendência detectadas pelo SuperTrend ajustado
- **Comprado/Vendido**: Ambos (configurável)
- **Critérios de saída**: mudança de tendência oposta
- **Stops**: Não
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `VegasWindow` = 100
  - `SuperTrendMultiplier` = 5
  - `VolatilityAdjustment` = 5
  - `TradeDirection` = "Both"
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
