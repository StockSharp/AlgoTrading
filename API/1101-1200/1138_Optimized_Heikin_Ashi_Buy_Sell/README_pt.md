# Estratégia Heikin-Ashi Otimizada com Opções de Compra/Venda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os candles Heikin-Ashi suavizam os dados de preço e destacam a direção da tendência. Esta estratégia opera em uma única direção por vez: ou compras em candles verdes ou vendas em candles vermelhos dentro de um intervalo de datas definido pelo usuário. Níveis opcionais de stop-loss e take-profit oferecem controle de risco.

## Detalhes

- **Critérios de entrada**: Mudança de cor do candle Heikin-Ashi.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Sinal oposto ou níveis de stop.
- **Stops**: Opcional, baseado em percentual.
- **Valores padrão**:
  - `CandleType` = 1 day
  - `StartDate` = 2023-01-01
  - `EndDate` = 2024-01-01
  - `TradeType` = BuyOnly
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
- **Filtros**:
  - Categoria: Tendência
  - Direção: Configurável
  - Indicadores: Heikin-Ashi
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Intervalo de datas
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

