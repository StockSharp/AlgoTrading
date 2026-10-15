# Estratégia de Momentum em Commodities
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Commodity Momentum** compra commodities com o momentum mais forte de 12 meses (ignorando o mês mais recente).
As posições são rebalanceadas no primeiro dia de negociação de cada mês.

Os testes indicam um retorno anual médio de aproximadamente 10%. Tem melhor desempenho em mercados de commodities diversificados.

As posições são ajustadas mensalmente; nenhum sinal intradiário é utilizado.

## Detalhes
- **Critérios de entrada**: Comprar as `TopN` principais commodities por momentum de 12 meses excluindo o último mês.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Rebalanceamento na próxima data programada.
- **Stops**: Sem lógica de stop explícita.
- **Valores padrão**:
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Momentum
  - Direção: Somente comprado
  - Indicadores: Price
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
