# Estratégia de Carry Trade em FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia cambial classifica um universo de instrumentos de divisas pelo diferencial de taxas de juros entre a moeda base e a moeda cotada. No início de cada mês, vai comprado nos `TopK` símbolos de maior carry e vendido nos `TopK` de menor carry. Os lucros visam capturar o carry positivo nas posições compradas enquanto paga o carry negativo nas vendidas.

Os diferenciais de taxas de juros são obtidos dos dados de rendimento de cada ativo. As posições são dimensionadas de forma igualitária e rebalanceadas mensalmente; qualquer instrumento que saia dos grupos superior ou inferior é encerrado e substituído.

## Detalhes

- **Critérios de entrada**:
  - No primeiro dia útil do mês, calcular o diferencial de taxas de juros para cada divisa.
  - Ir comprado nas `TopK` divisas com maior carry e vendido nas `TopK` com menor carry se os valores das ordens superarem `MinTradeUsd`.
- **Comprado/Vendido**: Comprado em carry alto, vendido em carry baixo.
- **Critérios de saída**: As posições são encerradas quando uma divisa sai dos grupos selecionados no próximo rebalanceamento.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Universe` – lista de instrumentos de divisas.
  - `TopK` = 3.
  - `CandleType` = 1 dia.
  - `MinTradeUsd` – valor mínimo de negociação.
- **Filtros**:
  - Categoria: Carry.
  - Direção: Comprado e vendido.
  - Período: Mensal.
  - Rebalanceamento: Mensal.

