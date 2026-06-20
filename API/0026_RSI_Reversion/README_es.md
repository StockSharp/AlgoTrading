# RSI Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la reversión a la media del RSI

Las pruebas indican un rendimiento anual promedio de aproximadamente 115%. Funciona mejor en el mercado de acciones.

RSI Reversion asume que el precio revertirá después de alcanzar valores extremos del RSI. Cuando el RSI cae por debajo del umbral inferior, compra; cuando está por encima del umbral superior, vende. Las posiciones se cierran cuando el RSI regresa hacia niveles neutros.

Los extremos pueden calibrarse para adaptarse a varios mercados. Usar filtros adicionales como la dirección de la tendencia ayuda a evitar desvanecerse ante movimientos fuertes demasiado pronto.


## Detalles

- **Criterios de entrada**: Señales basadas en RSI.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `OversoldThreshold` = 30m
  - `OverboughtThreshold` = 70m
  - `ExitLevel` = 50m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

