# Estratégia Larry Connors Bollinger %B
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia segue a abordagem %B de Larry Connors. Compra quando o preço está em tendência de alta acima da SMA de 200 períodos e o valor de Bollinger %B permanece abaixo de um limiar por três velas consecutivas. As posições são fechadas quando %B sobe acima de um limiar superior.

A configuração padrão tem como alvo velas diárias.

## Detalhes

- **Critérios de entrada**: Fechamento acima da SMA200 e %B abaixo de `LowPercentB` por três velas consecutivas.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: %B cruza acima de `HighPercentB` ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `SmaPeriod` = 200
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `LowPercentB` = 0.2m
  - `HighPercentB` = 0.8m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: Bollinger Bands, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
