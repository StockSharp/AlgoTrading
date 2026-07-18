# MA con Función Logística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MA con Función Logística es una estrategia de media móvil que usa una MA rápida y una lenta para las entradas y admite salidas basadas en porcentaje o en probabilidad logística.

## Detalles
- **Datos**: Velas de precios.
- **Criterios de entrada**:
  - **Largo**: Cierre > MA rápida y MA rápida > MA lenta.
  - **Corto**: Cierre < MA rápida y MA rápida < MA lenta.
- **Criterios de salida**: Objetivos porcentuales o umbrales de probabilidad logística.
- **Stops**: Salidas basadas en porcentaje o en probabilidad logística.
- **Valores predeterminados**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `MaType` = MaTypeEnum.EMA
  - `ExitType` = ExitTypeEnum.Percent
  - `TakeProfitPercent` = 20
  - `StopLossPercent` = 5
  - `LogisticSlope` = 10
  - `LogisticMidpoint` = 0
  - `TakeProfitProbability` = 0.8
  - `StopLossProbability` = 0.2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: MA
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
