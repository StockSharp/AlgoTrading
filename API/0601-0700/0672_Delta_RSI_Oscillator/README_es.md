# Oscilador Delta-RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador Delta-RSI, definido como el cambio del RSI suavizado con una EMA. Las señales se generan cuando el delta cruza el cero, cruza su línea de señal o cambia de dirección. Las salidas reflejan la condición seleccionada.

## Detalles

- **Criterios de entrada**: Basado en `BuyCondition` (cruce de cero, cruce de línea de señal o cambio de dirección) en Delta-RSI.
- **Largo/Corto**: Ambos, controlado por `UseLong` y `UseShort`.
- **Criterios de salida**: Basado en `ExitCondition` en Delta-RSI.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RsiLength` = 21
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI, EMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
