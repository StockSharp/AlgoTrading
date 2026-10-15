# Estratégia de Sistema de Trading Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o Simple Trading System do MetaTrader. Usa uma média móvel deslocada por várias barras e compara o fechamento atual com fechamentos anteriores para detectar reversões de tendência de curto prazo. Um sinal de compra ocorre quando a média móvel está abaixo do seu valor `MaShift` barras atrás e o fechamento está entre os fechamentos de `MaShift` e `MaPeriod + MaShift` barras atrás enquanto a vela é de baixa. Um sinal de venda é o espelho oposto. Dependendo dos parâmetros, a estratégia pode abrir e/ou fechar posições compradas ou vendidas quando os sinais aparecem. Níveis opcionais de stop-loss e take-profit podem ser configurados.

## Detalhes

- **Critérios de entrada:**
  - **Comprado**: `MA(t) <= MA(t+MaShift)` && `Close(t) >= Close(t+MaShift)` && `Close(t) <= Close(t+MaPeriod+MaShift)` && `Close(t) < Open(t)`
  - **Vendido**: `MA(t) >= MA(t+MaShift)` && `Close(t) <= Close(t+MaShift)` && `Close(t) >= Close(t+MaPeriod+MaShift)` && `Close(t) > Open(t)`
- **Comprado/Vendido**: Ambos os lados dependendo de `BuyPositionOpen` e `SellPositionOpen`.
- **Critérios de saída**: O sinal oposto aciona o fechamento se `BuyPositionClose` ou `SellPositionClose` estiver habilitado.
- **Stops**: Opcional. `StopLoss` e `TakeProfit` em unidades absolutas de preço via `StartProtection`.
- **Valores padrão:**
  - `MaType` = EMA
  - `MaPeriod` = 2
  - `MaShift` = 4
  - `PriceType` = Close
  - `CandleType` = velas de 6 horas
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `Volume` = 1
- **Filtros:**
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Média Móvel
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
