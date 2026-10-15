# Estrategia BTCUSD con SLTP Ajustable
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera BTCUSD usando un cruce entre SMA(10) y SMA(25) con un filtro EMA(150). Las entradas largas esperan un retroceso: tras el cruce se rastrea un porcentaje de retroceso y se abre una posición larga cuando el precio cruza de nuevo por encima de ese nivel. Las entradas cortas se activan inmediatamente ante un cruce bajista mientras el precio está por debajo de la EMA.

Las salidas usan distancias ajustables de take-profit, stop-loss y break-even. Una posición larga también se cierra si SMA(10) cruza por debajo de SMA(25) mientras el precio está por debajo de EMA(150).

## Detalles

- **Criterios de entrada**:
  - Largo: SMA(10) cruza por encima de SMA(25), luego el precio retrocede un porcentaje fijo y cruza por encima del nivel de retroceso.
  - Corto: SMA(10) cruza por debajo de SMA(25) mientras el precio está por debajo de EMA(150).
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - Distancias configurables de take-profit, stop-loss y break-even.
  - Salida larga cuando SMA(10) cruza por debajo de SMA(25) bajo EMA(150).
- **Stops**: Sí, ajustables en puntos.
- **Valores predeterminados**:
  - `FastSmaLength` = 10
  - `SlowSmaLength` = 25
  - `EmaFilterLength` = 150
  - `TakeProfitDistance` = 1000
  - `StopLossDistance` = 250
  - `BreakEvenTrigger` = 500
  - `RetracementPercentage` = 0.01
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: SMA, EMA
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
