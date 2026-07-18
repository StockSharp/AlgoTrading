# Estratégia Tuga Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Tuga Supertrend é uma estratégia somente comprada baseada no indicador SuperTrend. Ela entra em uma posição comprada quando a direção do SuperTrend muda para baixo e sai quando a direção vira para cima.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: A direção do SuperTrend muda de cima para baixo dentro da janela de datas.
- **Critérios de saída**: A direção do SuperTrend muda de baixo para cima.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `AtrPeriod` = 10
  - `Factor` = 3.0
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: SuperTrend, ATR
  - Complexidade: Baixo
  - Nível de risco: Médio
