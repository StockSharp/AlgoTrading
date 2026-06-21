# Estratégia de Comprar e Manter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em uma única posição comprada na data de início especificada e a mantém até a data de fim, implementando uma abordagem simples de comprar e manter.

## Detalhes

- **Critérios de entrada**:
  - Quando o tempo de um candle é igual ou posterior à data de início, a estratégia compra uma vez.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Quando o tempo de um candle atinge ou supera a data de fim, a posição é fechada.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Data de início = 2018-01-01.
  - Data de fim = 2069-12-31.
- **Filtros**:
  - Categoria: Buy and Hold.
  - Direção: Comprado.
  - Indicadores: Nenhum.
  - Stops: Não.
  - Complexidade: Baixo.
  - Período: Qualquer.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Alto.
