# Estratégia de Filtro de Intervalo DW
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um filtro de intervalo baseado em ATR semelhante ao Range Filter de Donovan Wall. O filtro ignora movimentos de preço menores, movendo-se apenas quando o preço excede um intervalo baseado em volatilidade. Uma posição comprada é aberta quando o fechamento está acima da banda superior, enquanto uma posição vendida é aberta quando o fechamento está abaixo da banda inferior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento acima da banda superior.
  - **Vendido**: Fechamento abaixo da banda inferior.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Rompimento da banda oposta.
- **Stops**: Não.
- **Valores padrão**:
  - `RangePeriod` = 14
  - `RangeMultiplier` = 2.618
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
