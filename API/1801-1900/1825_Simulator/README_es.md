# Estrategia Simulador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera cruces de EMA con stop-loss y take-profit opcionales. Compra cuando la EMA rápida cruza hacia arriba de la EMA lenta y vende cuando la EMA rápida cruza hacia abajo de la EMA lenta. Las señales opuestas o los objetivos de precio cierran las posiciones.

## Detalles

- **Criterios de entrada**:
  - Largo: EMA rápida cruza hacia arriba la EMA lenta
  - Corto: EMA rápida cruza hacia abajo la EMA lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cruce de EMA opuesto
  - Largo: el precio alcanza el take-profit o el stop-loss
  - Corto: el precio alcanza el take-profit o el stop-loss
- **Stops**: Desplazamientos de precio fijos
- **Valores predeterminados**:
  - `FastPeriod` = 13
  - `SlowPeriod` = 50
  - `StopLoss` = 0.005m
  - `TakeProfit` = 0.005m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
