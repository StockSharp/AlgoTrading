# Modelo Dinámico de Diferencial de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Dynamic Volatility Differential Model (DVDM)** compara la volatilidad implícita con la volatilidad histórica. Abre largo cuando la volatilidad implícita supera la volatilidad realizada por un umbral dinámico de desviación estándar y entra corto cuando el diferencial cae por debajo del umbral negativo.

Las señales usan datos diarios y no dependen de stops.

## Detalles
- **Criterios de entrada**: Diferencial de volatilidad por encima/debajo de los umbrales dinámicos de desviación estándar.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Diferencial de volatilidad cruzando la línea cero.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length = 5`
  - `StdevMultiplier = 7.1m`
  - `VolatilitySecurity = "TVC:VIX"`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: StandardDeviation
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
