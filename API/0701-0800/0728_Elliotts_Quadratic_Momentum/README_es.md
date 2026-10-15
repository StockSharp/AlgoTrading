# Momentum Cuadrático de Elliott
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Elliott's Quadratic Momentum** combina múltiples indicadores SuperTrend para capturar el momentum inspirado en las ondas de Elliott.

La estrategia entra largo cuando las cuatro líneas SuperTrend señalan una tendencia alcista y entra corto cuando todas señalan una tendencia bajista. Las posiciones se cierran cuando cualquier SuperTrend revierte su dirección.

## Detalles
- **Criterios de entrada**: Todos los indicadores SuperTrend alineados en la misma dirección.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cualquier SuperTrend gira contra la posición.
- **Stops**: Sin stops explícitos.
- **Valores predeterminados**:
  - `AtrLength1 = 7`
  - `Multiplier1 = 4.0m`
  - `AtrLength2 = 14`
  - `Multiplier2 = 3.618m`
  - `AtrLength3 = 21`
  - `Multiplier3 = 3.5m`
  - `AtrLength4 = 28`
  - `Multiplier4 = 3.382m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
