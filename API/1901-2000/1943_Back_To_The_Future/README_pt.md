# Estratégia Back to the Future
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de momentum compara o preço de fechamento atual com o preço de um número especificado de minutos atrás. Quando o preço avança além de um limiar definido em relação ao preço histórico, o sistema abre uma posição comprada. Por outro lado, quando o preço cai abaixo do limiar negativo, abre uma posição vendida. A abordagem assume que movimentos fortes afastando-se do preço passado indicam tendências emergentes.

A estratégia opera em candles completados e funciona com qualquer instrumento e período suportado pelo StockSharp. Níveis integrados de take-profit e stop-loss gerenciam o risco assim que uma posição é aberta. Uma fila de preços passados mantém um histórico contínuo para avaliar a diferença de preço.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close(t) - Close(t-Δ) > BarSize`.
  - **Vendido**: `Close(t) - Close(t-Δ) < -BarSize`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: `Close >= Entry + TakeProfit` ou `Close <= Entry - StopLoss`.
  - **Vendido**: `Close <= Entry - TakeProfit` ou `Close >= Entry + StopLoss`.
- **Stops**: Sim, take-profit e stop-loss fixos em unidades de preço.
- **Valores padrão**:
  - `BarSize = 0.25`
  - `HistoryMinutes = 60`
  - `TakeProfit = 10`
  - `StopLoss = 5000`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
