# Estratégia de Mapa de Calor Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Stochastic Heat Map calcula a média de um conjunto de osciladores Stochastic com períodos crescentes.
A leitura combinada é suavizada novamente para formar uma linha rápida e uma lenta.
As operações vão compradas quando a linha rápida cruza acima da lenta e vendidas no cruzamento oposto.

## Detalhes

- **Critérios de entrada**: cruzamento da linha rápida/lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `Increment` = 10
  - `SmoothFast` = 2
  - `SmoothSlow` = 21
  - `PlotNumber` = 28
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
