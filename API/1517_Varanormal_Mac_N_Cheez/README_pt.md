# Estratégia Varanormal Mac N Cheez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de SMA com stop trailing e meta de lucro diário.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SMA rápida cruza acima da SMA lenta.
  - **Vendido**: SMA rápida cruza abaixo da SMA lenta.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Stop trailing ou stop loss fixo.
  - Meta de lucro diário fecha todas as posições.
- **Stops**: Sim, fixo e trailing.
- **Valores padrão**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `DailyTarget` = 200
  - `StopLossAmount` = 100
  - `TrailOffset` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
