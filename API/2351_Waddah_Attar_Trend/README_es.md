# Waddah Attar Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el experto MQL original "Exp_Waddah_Attar_Trend" a la API de alto nivel de StockSharp. Utiliza el indicador Waddah Attar Trend, que multiplica la diferencia entre dos medias móviles exponenciales (rápida y lenta) por una media móvil de suavizado adicional. El indicador emite un estado de color: verde cuando el valor de tendencia sube y magenta cuando cae. Un cambio de este color desencadena operaciones.

Las posiciones largas se abren cuando el color cambia de descendente a ascendente. Las posiciones cortas se abren cuando cambia de ascendente a descendente. La estrategia opera en ambas direcciones y admite stop-loss y take-profit expresados como porcentajes del precio de entrada.

## Detalles

- **Criterios de entrada**: Cambio de color de Waddah Attar Trend (diferencia MACD multiplicada por MA).
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cambio de color opuesto o stops de protección.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `MaLength` = 9
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `StopLossPercent` = 1.0
  - `TakeProfitPercent` = 2.0
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
