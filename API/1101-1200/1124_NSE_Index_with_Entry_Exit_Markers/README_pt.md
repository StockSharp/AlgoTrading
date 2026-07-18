# Estratégia de Índice NSE com Marcadores de Entrada e Saída
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra quando o preço está acima de uma SMA de tendência e o RSI cruza para cima acima do nível de sobrevenda. Um stop loss e take profit baseados em ATR gerenciam a posição.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço está acima da SMA e o RSI cruza para cima acima do nível de sobrevenda.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - fechar a posição comprada quando o preço atinge o stop baseado em ATR ou o take profit.
- **Stops**: Stop loss e take profit baseados em ATR.
- **Valores padrão**:
  - `SmaPeriod` = 200.
  - `RsiPeriod` = 14.
  - `RsiOversold` = 40.
  - `AtrPeriod` = 14.
  - `AtrMultiplier` = 1.5.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: SMA, RSI, ATR
  - Stops: Baseado em ATR
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
