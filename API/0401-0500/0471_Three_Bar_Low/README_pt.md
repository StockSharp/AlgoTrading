# Estratégia de Mínimo de 3 Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Mínimo de 3 Barras compra quando o preço de fechamento cai abaixo do fechamento mínimo das três barras anteriores e sai quando o preço fecha acima do fechamento máximo das sete barras anteriores. Um filtro EMA opcional pode exigir que o preço permaneça acima de uma média de longo prazo antes de permitir entradas.

## Detalhes

- **Critérios de entrada**:
  - O preço de fechamento está abaixo do fechamento mínimo das três barras anteriores.
  - Opcional: o preço de fechamento está acima da EMA quando o filtro está ativado.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - O preço de fechamento está acima do fechamento máximo das sete barras anteriores.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MaPeriod` = 200
  - `LowestLength` = 3
  - `HighestLength` = 7
  - `UseEmaFilter` = false
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Long
  - Indicadores: EMA, Highest/Lowest
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
