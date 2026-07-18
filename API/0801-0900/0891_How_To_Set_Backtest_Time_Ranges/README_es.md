# Cómo Establecer Rangos de Tiempo para Backtesting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia demuestra cómo restringir el trading a ventanas de fecha y tiempo intradía específicas. Entra en largo cuando una SMA rápida cruza por encima de una SMA lenta y sale en el cruce opuesto.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: La SMA rápida cruza por encima de la SMA lenta dentro de los rangos de fecha y tiempo de entrada seleccionados.
- **Criterios de salida**: La SMA rápida cruza por debajo de la SMA lenta dentro de los rangos de fecha y tiempo de salida seleccionados.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `FromDate` = 2021-01-01
  - `ThruDate` = 2112-01-01
  - `EntryStart` = 00:00
  - `EntryEnd` = 00:00
  - `ExitStart` = 00:00
  - `ExitEnd` = 00:00
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: SMA
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
