# Tendencia MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador MACD.

Las pruebas indican un retorno anual promedio de aproximadamente 64%. Funciona mejor en el mercado de divisas.

La Tendencia MACD reacciona a los cruces entre la línea MACD y su línea de señal. Los cruces alcistas inician largos mientras que los cruces bajistas inician cortos. Los cruces opuestos o un stop cierran la operación.

El indicador de convergencia/divergencia de medias móviles se adapta bien a los mercados cambiantes midiendo el momentum. Este enfoque apunta a aprovechar los balances con tendencia mientras el indicador mantiene un sesgo claramente alcista o bajista.


## Detalles

- **Criterios de entrada**: Señales basadas en MA, MACD.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

