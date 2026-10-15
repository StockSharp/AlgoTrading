# Exp Posición de Precio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Exp Price Position** adapta el asesor experto original de MetaTrader que combina la ubicación del precio y un filtro de tendencia escalonada.
Evalúa la relación entre dos medias móviles medianas para localizar el último nivel de oscilación y luego verifica un par de medias móviles suavizadas rápida y lenta para determinar la dirección de la tendencia.
Las órdenes se abren solo cuando tanto la posición del precio como la tendencia escalonada concuerdan con la estructura de la vela actual.

La estrategia está diseñada para mercados donde los cambios de tendencia ocurren después de que el precio retrocede a un nivel mediano dinámico. Se aplica un stop dinámico y una relación de toma de beneficios para gestionar el riesgo.

## Detalles

- **Criterios de entrada**: Precio por encima del último nivel de oscilación con tendencia escalonada alcista para operaciones largas; por debajo con tendencia escalonada bajista para operaciones cortas.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop de protección.
- **Stops**: Sí, mediante trailing stop con relación de toma de beneficios.
- **Valores predeterminados**:
  - `FastPeriod` = 2
  - `SlowPeriod` = 30
  - `MedianFastPeriod` = 26
  - `MedianSlowPeriod` = 20
  - `TpSlRatio` = 3m
  - `TrailingStopPips` = 10m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Smoothed Moving Average, Simple Moving Average
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
