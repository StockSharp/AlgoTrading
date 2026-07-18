# Estratégia AI Supertrend Pivot Percentile
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina dois indicadores Supertrend com um filtro ADX e um filtro de percentil de pivô de Williams %R. Uma posição comprada é aberta quando o preço está acima de ambos os Supertrends, o ADX confirma uma tendência forte e o Williams %R está acima de -50. As posições vendidas usam as condições opostas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço acima de ambos os Supertrends, ADX > limiar, Williams %R > -50.
  - **Vendido**: Preço abaixo de ambos os Supertrends, ADX > limiar, Williams %R < -50.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Take-profit e stop-loss baseados em percentual.
- **Valores padrão**:
  - `Length1` = 10
  - `Factor1` = 3
  - `Length2` = 20
  - `Factor2` = 4
  - `AdxLength` = 14
  - `AdxThreshold` = 20
  - `PivotLength` = 14
  - `TpPercent` = 2
  - `SlPercent` = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, ADX, Williams %R
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
