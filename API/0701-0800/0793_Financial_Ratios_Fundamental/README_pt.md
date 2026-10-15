# Estratégia Fundamental de Índices Financeiros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia analisa índices financeiros trimestrais para avaliar os fundamentos de uma empresa. Examina o índice de liquidez corrente, a cobertura de juros, o giro de contas a pagar e a margem bruta, abrindo posições compradas quando qualquer um desses índices melhora em relação ao período anterior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `currentRatio > previousCurrent` OU `interestCoverage < previousInterest` OU `payableTurnover > previousPayable` OU `grossMargin > previousGross`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - **Comprado**: `currentRatio < previousCurrent` OU `interestCoverage > previousInterest` OU `payableTurnover < previousPayable` OU `grossMargin < previousGross`.
- **Stops**: Não.
- **Valores padrão**:
  - `Candle Type` = candles diários.
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
