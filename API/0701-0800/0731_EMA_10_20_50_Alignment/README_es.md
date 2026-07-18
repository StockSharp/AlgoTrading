# Alineación EMA 10/20/50
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia solo largos entra cuando EMA(10) > EMA(20) > EMA(50) y sale cuando las EMA se alinean en orden descendente. El trading está restringido a un rango de fechas configurable.

## Detalles

- **Criterios de entrada**: EMA(10) por encima de EMA(20) por encima de EMA(50) dentro del rango de fechas especificado.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Las EMA se alinean a la baja (EMA(10) < EMA(20) < EMA(50)).
- **Stops**: No.
- **Valores predeterminados**:
  - `StartTime` = new DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `EndTime` = new DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
