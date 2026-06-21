# Estratégia de Reversão à Média com Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra quando o preço fecha abaixo da banda inferior de Bollinger e sai quando o preço fecha acima da banda superior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento abaixo da banda inferior.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento acima da banda superior.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Comprimento de Bollinger Bands 20.
  - Multiplicador 2.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Bollinger Bands
  - Stops: Não
  - Complexidade: Simples
  - Período: Curto prazo
