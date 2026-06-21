# Estratégia de Média Móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra em uma posição comprada quando uma média móvel curta cruza acima de uma média móvel longa do tipo de preço selecionado. A posição é fechada quando a média curta cruza de volta abaixo da longa.

## Detalhes
- **Critérios de entrada:** MA curta cruza acima da MA longa.
- **Critérios de saída:** MA curta cruza abaixo da MA longa.
- **Indicadores:** SMA, EMA, DEMA, TEMA, WMA, VWMA.
- **Fonte de preço:** Close, High, Open, Low, Typical, Center.
- **Stops:** Nenhum.
- **Valores padrão:**
  - `MaType` = EMA
  - `ShortLength` = 1
  - `LongLength` = 20
  - `PriceType` = Typical
  - `CandleType` = 1 minute
- **Filtros:**
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Média móvel
  - Stops: Não
  - Complexidade: Simples
  - Nível de risco: Médio
