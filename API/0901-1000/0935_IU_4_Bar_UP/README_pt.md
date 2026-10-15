# Estratégia IU 4 Barras de Alta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia IU 4 Barras de Alta é uma abordagem somente comprado que compra após quatro velas de alta consecutivas quando o preço está acima do indicador SuperTrend.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Quatro velas de alta consecutivas e fechamento acima do SuperTrend.
- **Critérios de saída**: Fechamento abaixo do SuperTrend.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `SupertrendLength` = 14
  - `SupertrendMultiplier` = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: SuperTrend
  - Complexidade: Baixo
  - Nível de risco: Médio
