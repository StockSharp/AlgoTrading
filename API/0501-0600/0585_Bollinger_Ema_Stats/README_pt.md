# Estratégia de Bollinger EMA Stats
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza dois conjuntos de Bandas de Bollinger para definir zonas de entrada e stop, e uma EMA como alvo de saída.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Close < banda inferior de Bollinger (multiplicador de entrada).
  - **Vendido**: Close > banda superior de Bollinger (multiplicador de entrada).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Alvo de lucro na EMA.
  - Stop loss na Banda de Bollinger mais ampla.
- **Stops**: Sim.
- **Valores padrão**:
  - `BB Length` = 20
  - `Entry StdDev Mult` = 2.0
  - `Stop StdDev Mult` = 3.0
  - `EMA Exit Period` = 20
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: Bollinger Bands, EMA
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Médio prazo
