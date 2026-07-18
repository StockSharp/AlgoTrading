# Estrategia VIDYA ProTrend de Beneficio Multi-Nivel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que usa promedios VIDYA rápidos y lentos con un filtro de Bandas de Bollinger.
Opcionalmente se colocan órdenes de take profit en múltiples niveles usando múltiplos de ATR y objetivos porcentuales.

## Detalles

- **Criterios de entrada**: VIDYA rápida por encima de la VIDYA lenta con el precio fuera del filtro de Bollinger
- **Largo/Corto**: Ambos
- **Criterios de salida**: pendiente u cruce opuesto
- **Stops**: No
- **Valores predeterminados**:
  - `FastVidyaLength` = 10
  - `SlowVidyaLength` = 30
  - `MinSlopeThreshold` = 0.05
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: VIDYA, Bollinger Bands, ATR
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
