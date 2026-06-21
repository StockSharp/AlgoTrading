# Estratégia Trailing Monster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina detecção de tendência KAMA com filtro RSI e um trailing stop. As posições são abertas quando o RSI cruza níveis extremos na direção da tendência KAMA. Após um atraso, um trailing stop percentual protege os lucros.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI > `RsiOverbought`, fechamento acima da SMA, KAMA em alta
  - **Vendido**: RSI < `RsiOversold`, fechamento abaixo da SMA, KAMA em queda
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Trailing stop percentual após `DelayBars`
- **Stops**: Trailing stop em percentual
- **Valores padrão**:
  - `KamaLength` = 40
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `SmaLength` = 200
  - `BarsBetweenEntries` = 3
  - `TrailingStopPct` = 12m
  - `DelayBars` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: KAMA, RSI, SMA
  - Stops: Trailing
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
