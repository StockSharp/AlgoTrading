# Estrategia Exp X2MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Exp X2MA opera en los puntos de giro de una media móvil de doble suavizado.
El precio se suaviza primero con una media móvil simple y luego con una media móvil Jurik.
Cuando la línea suavizada forma un mínimo local, la estrategia compra y cierra posiciones cortas.
Cuando forma un máximo local, la estrategia vende y cierra posiciones largas.
Un stop loss fijo y take profit opcionales protegen las posiciones abiertas.

## Detalles
- **Datos**: Velas de precio (por defecto 4 horas).
- **Criterios de entrada**:
  - **Largo**: El valor anterior de X2MA es menor que el más antiguo y el valor actual gira hacia arriba.
  - **Corto**: El valor anterior de X2MA es mayor que el más antiguo y el valor actual gira hacia abajo.
- **Criterios de salida**: Extremo opuesto, stop loss o take profit.
- **Stops**: Stop loss fijo y take profit en puntos.
- **Valores predeterminados**:
  - `FirstMaLength` = 12
  - `SecondMaLength` = 5
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
- **Filtros**:
  - Categoría: Reversión de tendencia
  - Dirección: Largo y Corto
  - Indicadores: SMA, JurikMovingAverage
  - Stops: Sí
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
