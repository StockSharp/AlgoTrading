# Liquidex Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Liquidex Keltner** opera rupturas de los Canales Keltner con un filtro de tendencia por media móvil.
Las operaciones solo se permiten durante las horas especificadas y pueden confirmarse opcionalmente por la dirección del RSI.
El stop-loss y el take-profit se gestionan mediante porcentajes fijos.

## Detalles
- **Criterios de entrada**:
  - El precio cruza por encima de la banda superior Keltner y cierra por encima de la media móvil.
  - El precio cruza por debajo de la banda inferior Keltner y cierra por debajo de la media móvil.
  - El cuerpo de la vela debe superar `RangeFilter`.
  - Cuando `UseRsiFilter` está habilitado, el RSI debe estar por encima de 50 para largos y por debajo de 50 para cortos.
  - La hora actual debe estar entre `EntryHourFrom` y `EntryHourTo`, y antes de `FridayEndHour` los viernes.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Sí, basados en porcentaje mediante `StartProtection`.
- **Valores predeterminados**:
  - `MaPeriod = 7`
  - `RangeFilter = 10m`
  - `StopLoss = 1m`
  - `TakeProfit = 2m`
  - `UseKeltnerFilter = true`
  - `KeltnerPeriod = 6`
  - `KeltnerMultiplier = 1m`
  - `UseRsiFilter = false`
  - `RsiPeriod = 14`
  - `EntryHourFrom = 2`
  - `EntryHourTo = 24`
  - `FridayEndHour = 22`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: MA, Keltner, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
