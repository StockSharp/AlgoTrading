# Estrategia CBC con Confirmación de Tendencia y Stop Loss Separado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el estado de cambio de color de barra (CBC) para detectar giros cuando el precio rompe el máximo o mínimo de la vela anterior. Las entradas requieren confirmación de tendencia mediante EMA y VWAP y están restringidas a una ventana de sesión de trading. Las salidas aplican un objetivo de ganancia basado en ATR y utilizan los extremos de la vela anterior como niveles de stop loss.

## Detalles

- **Criterios de entrada**: Giros CBC, filtro opcional de giros fuertes, EMA lenta relativa al VWAP, dentro del horario de trading.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take-profit con multiplicador ATR, stop loss en el máximo/mínimo de la vela anterior.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrLength` = 14
  - `ProfitTargetMultiplier` = 1.0m
  - `StrongFlipsOnly` = true
  - `EntryStartHour` = 10
  - `EntryEndHour` = 15
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, VWAP, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
