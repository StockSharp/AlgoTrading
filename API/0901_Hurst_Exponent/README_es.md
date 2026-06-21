# Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simple que opera basándose en un Hurst Exponent suavizado.  
El valor de Hurst se suaviza con una EMA y se compara con un umbral para determinar el régimen de mercado.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Hurst suavizado > Umbral
  - **Corto**: Hurst suavizado < Umbral
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - **Largo**: Hurst suavizado < Umbral
  - **Corto**: Hurst suavizado > Umbral
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `HurstPeriod = 100`
  - `SmoothLength = 10`
  - `Threshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5)`
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Hurst Exponent, EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
