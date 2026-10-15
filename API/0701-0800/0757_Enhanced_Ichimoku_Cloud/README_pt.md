# Estratégia Aprimorada de Nuvem Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia Ichimoku somente comprada com filtro de EMA de 171 dias. A estratégia compra quando o span A está acima do span B, o preço rompe a máxima de 25 barras atrás, Tenkan-sen está acima de Kijun-sen e o fechamento está acima da EMA. A posição é encerrada quando Tenkan cai abaixo de Kijun.

## Detalhes

- **Critérios de entrada**: spanA > spanB, close > high[25], Tenkan > Kijun, close > EMA.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Tenkan < Kijun.
- **Stops**: Não.
- **Valores padrão**:
  - `ConversionPeriods` = 7
  - `BasePeriods` = 211
  - `LaggingSpan2Periods` = 120
  - `Displacement` = 41
  - `EmaPeriod` = 171
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: Ichimoku, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
