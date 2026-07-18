# Estrategia Forex de Martillo y Hombre Colgado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera los patrones clásicos de reversión de velas japonesas: el martillo alcista y el hombre colgado bajista. Entra largo después de un martillo y corto después de un hombre colgado, manteniendo la posición durante un número fijo de barras.

La posición se cierra una vez que expira el período de retención o se activan los stops de protección.

## Detalles

- **Criterios de entrada**: Martillo para largo, hombre colgado para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Período de retención o stop-loss/take-profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `BodyLengthMultiplier` = 5
  - `ShadowRatio` = 1
  - `HoldPeriods` = 26
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
