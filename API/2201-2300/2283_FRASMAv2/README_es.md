# Estrategia FRASMAv2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la Media Móvil Simple Adaptativa Fractal (FRASMAv2).

Esta estrategia calcula una Media Móvil Simple Adaptativa Fractal usando el indicador Fractal Dimension. El color del indicador cambia según la pendiente: verde para subida, gris para lateral, magenta para bajada. La estrategia observa las transiciones de color en la última vela cerrada:

- Si el indicador era verde en la barra anterior y se vuelve no verde (gris o magenta) en la última barra, la estrategia cierra las posiciones cortas y abre una nueva posición larga.
- Si el indicador era magenta y se vuelve no magenta, la estrategia cierra las posiciones largas y abre una nueva posición corta.

La gestión del riesgo utiliza los parámetros de stop-loss y take-profit especificados en puntos.

## Detalles

- **Criterios de entrada**: Cambios de color de FRASMAv2.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Transición de color opuesta.
- **Stops**: Take profit y stop loss mediante módulo de protección.
- **Valores predeterminados**:
  - `Period` = 30
  - `TakeProfit` = 2000 puntos
  - `StopLoss` = 1000 puntos
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Reversión de tendencia
  - Dirección: Ambos
  - Indicadores: FractalDimension, FRASMAv2
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
