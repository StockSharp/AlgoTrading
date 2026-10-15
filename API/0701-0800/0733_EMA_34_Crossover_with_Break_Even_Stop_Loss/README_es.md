# Cruce de EMA 34 con Stop Loss en Punto de Equilibrio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **EMA 34 Crossover with Break Even Stop Loss** entra en largo cuando el precio cruza por encima de la EMA de 34 períodos. El stop loss se coloca en el mínimo de la vela anterior, el take profit es diez veces el riesgo, y el stop se mueve al punto de equilibrio después de que el precio alcanza tres veces el riesgo.

## Detalles
- **Criterios de entrada**: El cierre cruza por encima de EMA(34) desde abajo.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop loss en el mínimo anterior o take profit en 10× riesgo.
- **Stops**: Sí, stop de punto de equilibrio.
- **Valores predeterminados**:
  - `EmaPeriod = 34`
  - `TakeProfitMultiplier = 10m`
  - `BreakEvenMultiplier = 3m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
