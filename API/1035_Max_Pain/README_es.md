# Estrategia Max Pain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre posiciones largas cuando tanto el volumen como el movimiento de precio superan umbrales configurables y el índice VIX permanece por debajo de un nivel especificado. Se establece un stop-loss basado en volatilidad en la entrada y la posición se cierra después de un número fijo de períodos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: volumen mayor que el volumen promedio × `VolumeMultiplier` y cambio de precio mayor que el cierre anterior × `PriceChangeMultiplier` con VIX por debajo de `VixThreshold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Stop-loss en `StopLossMultiplier` × volatilidad por debajo del precio de entrada.
  - Cerrar posición después de `HoldPeriods` barras.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `LookbackPeriod` = 70.
  - `VolumeMultiplier` = 1.
  - `PriceChangeMultiplier` = 0.029.
  - `StopLossMultiplier` = 2.4.
  - `VixThreshold` = 44.
  - `HoldPeriods` = 8.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
  - `VixCandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Solo largos
  - Indicadores: Volumen, acción del precio, volatilidad
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
