# Estratégia LSMA: Cálculo Alternativo Rápido e Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa uma aproximação rápida da Média Móvel de Mínimos Quadrados (LSMA) calculada como `3 × WMA − 2 × SMA`. Uma posição comprada é aberta quando o preço cruza acima da LSMA, e uma posição vendida quando cruza abaixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O fechamento cruza acima da LSMA.
  - **Vendido**: O fechamento cruza abaixo da LSMA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Comprimento 25.
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: WMA, SMA
  - Stops: Não
  - Complexidade: Simples
  - Período: Não especificado
