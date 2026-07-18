# Estrategia de la Vela de las 9:50
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera la vela de cinco minutos de las 9:50 AM de Nueva York. Después de que la vela se completa, entra en la dirección de la misma con un objetivo de beneficio fijo y stop definidos en ticks.

## Detalles
- **Criterios de entrada**: Dirección de la vela de cinco minutos de las 9:50 AM NY.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Alcanzar el objetivo o el stop.
- **Stops**: Stop y objetivo fijos.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TickSize` = 0.25
  - `TargetTicks` = 150
  - `StopTicks` = 200
- **Filtros**:
  - Categoría: Tiempo
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
