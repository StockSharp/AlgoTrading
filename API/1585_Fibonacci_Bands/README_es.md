# Estrategia de Bandas Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Expande un Canal Keltner por ratios Fibonacci y opera cuando el precio rompe la banda exterior con confirmación de RSI.

## Detalles

- **Criterios de entrada**: El precio cruza `fbUpper3` con RSI por encima de 60 para largo; cruza `fbLower3` con RSI por debajo de 40 para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El precio cruza de vuelta sobre la media móvil.
- **Stops**: No.
- **Valores predeterminados**:
  - `MaType` = WMA
  - `MaLength` = 233
  - `Fib1` = 1.618
  - `Fib2` = 2.618
  - `Fib3` = 4.236
  - `KcMultiplier` = 2
  - `KcLength` = 89
  - `RsiLength` = 14
  - `CandleType` = 5 minutes
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: MA, ATR, RSI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
