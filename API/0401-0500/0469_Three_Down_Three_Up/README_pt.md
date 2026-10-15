# Estratégia Three Down Three Up
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra após um número especificado de fechamentos consecutivos em baixa e fecha a posição após uma sequência de fechamentos em alta. Um filtro EMA opcional permite entradas somente quando o preço está acima da média móvel.

## Detalhes

- **Critérios de entrada**: O preço fecha abaixo da barra anterior por N barras. Condição opcional: preço acima da EMA.
- **Critérios de saída**: O preço fecha acima da barra anterior por M barras.
- **Comprado/Vendido**: Somente comprado.
- **Stops**: Nenhum.
- **Valores padrão**: Gatilho de compra = 3, gatilho de venda = 3, período EMA = 200.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Long
  - Indicadores: EMA (opcional)
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
