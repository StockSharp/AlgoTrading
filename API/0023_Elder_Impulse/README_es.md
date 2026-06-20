# Elder Impulse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el Sistema de Impulso de Elder

Las pruebas indican un rendimiento anual promedio de aproximadamente 106%. Funciona mejor en el mercado de acciones.

Elder Impulse combina la dirección de la EMA con el color del histograma del MACD. Las barras verdes por encima de la EMA impulsan posiciones largas, las barras rojas por debajo impulsan cortas, y las barras neutras señalan salidas.

Al combinar la dirección de la tendencia y el momentum, este enfoque mantiene a los traders en el lado correcto de los movimientos fuertes. Las salidas son sencillas, dependiendo del cambio de color del histograma o de la inversión de la pendiente de la EMA.


## Detalles

- **Criterios de entrada**: Señales basadas en MACD.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

