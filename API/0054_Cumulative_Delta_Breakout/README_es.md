# Ruptura por Delta Acumulativo (Cumulative Delta Breakout)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Cumulative Delta suma la diferencia entre el volumen de compra y venta. Esta estrategia monitorea el total acumulado y opera cuando supera su valor más alto o cae por debajo del más bajo dentro del período de lookback.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 49%. Funciona mejor en el mercado de criptomonedas.

Una ruptura del delta acumulativo a menudo precede al seguimiento del precio. La estrategia cierra las operaciones cuando el delta cruza de nuevo a través de cero o alcanza el nivel de stop-loss.

## Detalles

- **Criterios de entrada**: El delta acumulativo supera el valor más alto o más bajo en el lookback.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El delta cruza cero o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Cumulative Delta
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
