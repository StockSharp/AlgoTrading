# Color Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia basada en el oscilador Color Laguerre.

El oscilador Color Laguerre suaviza la serie de precios usando un filtro de Laguerre e indica la dirección de la tendencia mediante cambios de color. La estrategia compra cuando el oscilador se vuelve alcista y vende cuando se vuelve bajista. Los niveles extremos pueden forzar salidas si el impulso del precio se desvanece.

## Detalles

- **Criterios de entrada**: Oscilador cruzando el nivel medio.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Gamma` = 0.7m
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Oscilador
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

