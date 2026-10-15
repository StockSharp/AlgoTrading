# Estratégia Exp RSIOMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Exp RSIOMA usa o indicador RSI da média móvel (RSIOMA) para operar reversões de tendência e rompimentos. Os valores do RSI são suavizados por uma média móvel adicional para formar uma linha de sinal e um histograma. A estratégia suporta quatro modos:

1. **Breakdown** – opera quando o RSI cruza os níveis alto/baixo configurados.
2. **HistTwist** – opera quando o histograma muda de direção.
3. **SignalTwist** – opera quando a linha de sinal muda de direção.
4. **HistDisposition** – opera quando o histograma cruza a linha de sinal.

As posições podem ser abertas ou fechadas de forma independente para os lados comprado e vendido.

## Detalhes

- **Critérios de entrada**: depende do `Mode`
- **Comprado/Vendido**: ambos
- **Critérios de saída**: sinal oposto
- **Stops**: nenhum
- **Valores padrão**:
  - `CandleType` = 4 hour
  - `RsiPeriod` = 14
  - `SignalPeriod` = 21
  - `HighLevel` = 20
  - `LowLevel` = -20
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
