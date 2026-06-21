# Análisis de Hora del Día de Mateo LE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Abre una posición larga durante una ventana intradía específica y la cierra más tarde en el día.

Esta estrategia es útil para explorar los efectos de la hora del día.

## Detalles

- **Criterios de entrada**: El tiempo alcanza `StartTime` dentro del rango de fechas `From`-`Thru`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: El tiempo alcanza `EndTime` (antes de las 20:00).
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `StartTime` = 09:30
  - `EndTime` = 16:00
  - `From` = 2017-04-21
  - `Thru` = 2099-12-01
- **Filtros**:
  - Categoría: Basado en tiempo
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
