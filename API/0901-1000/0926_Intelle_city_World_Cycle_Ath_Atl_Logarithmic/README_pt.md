# Estratégia Logarítmica Intelle city World Cycle ATH ATL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia utiliza médias móveis escaladas para marcar sinais de máxima histórica (ATH) e mínima histórica (ATL) com base no conceito Pi Cycle.

O sistema vende quando a SMA longa escalada do ATH cruza abaixo da SMA curta, e compra quando a SMA longa escalada do ATL cruza acima da SMA curta.

## Detalhes

- **Critérios de entrada**: SMA longa escalada do ATH cruza abaixo da SMA curta do ATH para vender. SMA longa escalada do ATL cruza acima da SMA curta do ATL para comprar.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `AthLongLength` = 350
  - `AthShortLength` = 111
  - `AtlLongLength` = 471
  - `AtlShortLength` = 150
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
