# Estrategia de Pico Sigma por Hora del Día / Día de la Semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza la puntuación z del rendimiento para destacar grandes movimientos por hora con filtros opcionales por día.
Compra en picos y sale cuando la volatilidad se normaliza.

## Detalles

- **Criterios de entrada**: puntuación z absoluta >= `Threshold`
- **Largo/Corto**: Solo largos
- **Criterios de salida**: la puntuación z cae por debajo de `Threshold`
- **Stops**: No
- **Valores predeterminados**:
  - `Threshold` = 2.5
  - `AllDays` = false
  - `DayOfWeekFilter` = Monday
  - `StdevLength` = 20
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Largo
  - Indicadores: StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
