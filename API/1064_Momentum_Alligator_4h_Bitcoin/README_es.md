# Estrategia Momentum Alligator 4h Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Momentum Alligator 4h Bitcoin combina el Awesome Oscillator con el Alligator de Bill Williams en el marco temporal diario. Se abre una posición larga cuando el oscilador cruza por encima de su SMA de 5 períodos y el precio opera por encima de las tres líneas diarias del Alligator. Un stop loss dinámico utiliza el mayor valor entre una caída porcentual desde la entrada y la línea de mandíbula del Alligator. Tras una salida rentable, la estrategia omite las dos señales siguientes.

## Detalles

- **Criterios de entrada**: AO cruza por encima de su SMA de 5 períodos y el cierre está por encima de las líneas diarias del Alligator.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop loss dinámico en el máximo entre el stop porcentual y la mandíbula del Alligator.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StopLossPercent` = 0.02m
  - `CandleType` = TimeSpan.FromHours(4)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Solo largos
  - Indicadores: Awesome Oscillator, Alligator
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
