# Estratégia de Momentum do Fator ESG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia rotaciona entre um universo de ativos pontuados por métricas ambientais, sociais e de governança. No início de cada mês, classifica todos os símbolos pelo retorno acumulado e mantém apenas o de melhor desempenho. A premissa é que ativos que atraem capital ESG tendem a sustentar o momentum. Para evitar rotatividade excessiva, o algoritmo só opera quando o valor da posição supera um limite mínimo em dólares.

Durante o rebalanceamento, o sistema encerra qualquer posição existente e realoca no ativo de maior momentum. A carteira nunca usa alavancagem ou vendas a descoberto; está totalmente investida em um único ativo selecionado pela força do momentum.

## Detalhes

- **Critérios de entrada**:
  - No primeiro dia útil do mês, calcular o retorno total ao longo de `LookbackDays` para cada ativo.
  - Comprar o ativo com maior retorno se o tamanho da ordem for pelo menos `MinTradeUsd`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Todas as posições são encerradas em cada rebalanceamento mensal antes de abrir a nova posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Universe` – lista de símbolos focados em ESG.
  - `LookbackDays` = 252.
  - `CandleType` = 1 dia.
  - `MinTradeUsd` – valor mínimo de negociação.
- **Filtros**:
  - Categoria: Momentum.
  - Direção: Somente comprado.
  - Período: Médio prazo.
  - Rebalanceamento: Mensal.

