# Estrategia de Ruptura RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Ruptura RSI busca ráfagas de momentum cuando el Índice de Fuerza Relativa (RSI) supera su rango típico. Al medir las desviaciones del RSI respecto a su media móvil, el sistema pretende captar nuevas tendencias en su inicio.

Las pruebas indican un rendimiento anual promedio de aproximadamente 88%. Funciona mejor en el mercado de acciones.

Se abre una posición larga cuando el RSI cierra por encima de la media más `Multiplier` veces la desviación estándar. Se toma una posición corta cuando el RSI cae por debajo de la media menos ese multiplicador. Las posiciones se cierran una vez que el RSI cruza de vuelta por su valor medio.

Los traders de momentum pueden encontrar este enfoque útil para identificar rupturas tempranas mientras mantienen niveles de salida definidos. Un porcentaje de stop-loss protege contra reversiones repentinas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI > Avg + Multiplier * StdDev
  - **Corto**: RSI < Avg - Multiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando RSI < Avg
  - **Corto**: Salir cuando RSI > Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
