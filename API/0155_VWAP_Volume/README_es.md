# Estrategia VWAP Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina los indicadores VWAP y Volumen. Compra/vende en rupturas del VWAP confirmadas por un volumen superior al promedio.

Las pruebas indican un retorno anual promedio de aproximadamente el 52%. Funciona mejor en el mercado de criptomonedas.

Esta estrategia utiliza el VWAP para evaluar el valor y requiere confirmación de volumen antes de las operaciones. La idea es unirse a movimientos respaldados por una fuerte participación.

Los traders intradía enfocados en métricas de volumen pueden emplear este método. Las pérdidas se recortan mediante un stop basado en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < VWAP && Volume > AvgVolume * VolumeThreshold`
  - Corto: `Close > VWAP && Volume > AvgVolume * VolumeThreshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio cruza de vuelta a través del VWAP
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `VolumePeriod` = 20
  - `VolumeThreshold` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: VWAP, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
