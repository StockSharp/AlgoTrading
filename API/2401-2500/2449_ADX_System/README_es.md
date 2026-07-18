# Sistema ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Sistema ADX** opera utilizando el Average Directional Index y sus componentes DI. Abre una posición cuando el ADX sube y una de las líneas direccionales cruza por encima del ADX. Las posiciones incluyen niveles fijos de take-profit y stop-loss con un trailing stop para proteger el beneficio.

## Detalles

- **Criterios de entrada**
  - El ADX está subiendo (ADX anterior por debajo del actual).
  - Para operaciones **largas**: +DI anterior por debajo del ADX anterior y +DI actual por encima del ADX actual.
  - Para operaciones **cortas**: -DI anterior por debajo del ADX anterior y -DI actual por encima del ADX actual.
- **Criterios de salida**
  - Señal opuesta en las líneas ADX y DI.
  - El precio alcanza el nivel del trailing stop.
  - El precio alcanza el take-profit o stop-loss fijo.
- **Largo/Corto**: Ambas direcciones.
- **Stops**: Stop-loss fijo, take-profit y trailing stop en unidades de precio absolutas.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `TakeProfit` = 15
  - `StopLoss` = 100
  - `TrailingStop` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ADX, +DI, -DI
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
