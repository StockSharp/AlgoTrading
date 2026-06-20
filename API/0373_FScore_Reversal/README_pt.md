# Estratégia de Reversão por F-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina os fundamentos do Piotroski F-Score com a reversão de preço de curto prazo. A cada mês, compra a ação de pior desempenho entre aquelas com F-Score alto e, opcionalmente, vende a descoberto a de melhor desempenho com F-Score baixo. A premissa é que empresas fundamentalmente sólidas se recuperam após quedas temporárias, enquanto empresas fracas revertem após ralis.

No primeiro dia útil do mês, o algoritmo classifica o universo pelo retorno de um mês. Vai comprado no ativo com menor retorno com `FScore >= FHi` e, se disponível, vai vendido no ativo com maior retorno com `FScore <= FLo`. As posições são mantidas por um mês.

## Detalhes

- **Critérios de entrada**:
  - Comprado: entre ativos com `FScore >= FHi`, comprar o de menor retorno `Lookback` se o tamanho da operação >= `MinTradeUsd`.
  - Vendido (opcional): entre ativos com `FScore <= FLo`, vender a descoberto o de maior retorno `Lookback`.
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**: Encerrar todas as posições no próximo rebalanceamento mensal.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Universe` – ativos a avaliar.
  - `Lookback` = 21 dias.
  - `FHi` = 7.
  - `FLo` = 3.
  - `CandleType` = 1 dia.
  - `MinTradeUsd` – valor mínimo de negociação.
- **Filtros**:
  - Categoria: Reversão à média.
  - Direção: Comprado e vendido.
  - Período: Curto prazo.
  - Rebalanceamento: Mensal.

