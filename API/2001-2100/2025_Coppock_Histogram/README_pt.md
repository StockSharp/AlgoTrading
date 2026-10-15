# Estratégia de Histograma Coppock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões do Coppock Histogram. O indicador soma dois valores de Rate of Change e suaviza o resultado com uma média móvel. Quando o momentum vira para cima, a estratégia abre posições compradas e fecha as vendidas. Uma virada para baixo fecha as compradas e entra em vendidas. Os sinais são avaliados apenas em candles completados.

## Detalhes

- **Critérios de entrada**: Histograma Coppock com inclinação ascendente para compras ou descendente para vendas.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto fecha as posições abertas.
- **Stops**: Sem stop-loss ou take-profit explícitos.
- **Valores padrão**:
  - `Roc1Period` = 14
  - `Roc2Period` = 11
  - `SmoothPeriod` = 3
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(8)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RateOfChange, SimpleMovingAverage
  - Stops: Nenhum
  - Complexidade: Básico
  - Período: 8H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
