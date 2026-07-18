# Waddah Attar Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o especialista MQL original "Exp_Waddah_Attar_Trend" para a API de alto nível do StockSharp. Ela usa o indicador Waddah Attar Trend, que multiplica a diferença entre duas médias móveis exponenciais (rápida e lenta) por uma média móvel de suavização adicional. O indicador emite um estado de cor: verde quando o valor da tendência sobe e magenta quando cai. Uma mudança desta cor desencadeia negociações.

Posições compradas são abertas quando a cor muda de descendente para ascendente. Posições vendidas são abertas quando muda de ascendente para descendente. A estratégia opera em ambas as direções e suporta stop-loss e take-profit expressos como percentagens do preço de entrada.

## Detalhes

- **Critérios de entrada**: Mudança de cor do Waddah Attar Trend (diferença MACD multiplicada por MA).
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Mudança de cor oposta ou stops de proteção.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `MaLength` = 9
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `StopLossPercent` = 1.0
  - `TakeProfitPercent` = 2.0
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
