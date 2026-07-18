# Estratégia de Backtesting de Bot de Grade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa um bot de trading em grade que acumula posições compradas quando o preço cai para os níveis da grade e as fecha quando o preço sobe para a próxima linha. Os limites podem ser definidos manualmente ou calculados a partir de dados recentes.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço cruza abaixo de uma linha de grade sem ordem ativa
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - o preço cruza acima da próxima linha de grade
- **Stops**: Nenhum
- **Valores padrão**:
  - `AutoBounds` = true
  - `BoundSource` = "Hi & Low"
  - `BoundLookback` = 250
  - `BoundDeviation` = 0.10
  - `UpperBound` = 0.285
  - `LowerBound` = 0.225
  - `GridLines` = 30
- **Filtros**:
  - Categoria: Trading em range
  - Direção: Somente comprado
  - Indicadores: Highest, Lowest, SimpleMovingAverage
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
