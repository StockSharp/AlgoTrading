# Estrategia de cruce de velas promedio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia recrea el experto "Cruz de vela promedio" MetaTrader. Espera una barra completa donde la vela anterior cerró a través de una media móvil mientras que dos filtros de media móvil adicionales ya confirman la tendencia predominante. Sólo puede haber una posición activa a la vez. Inmediatamente después de abrir una operación, el algoritmo adjunta un stop-loss y un take-profit cuya distancia se deriva del stop especificado basado en pips. Esto hace que el comportamiento sea idéntico a la lógica del bloque original que se activa una vez por barra.

La lógica de entrada lee datos históricos de la barra en lugar de ticks inacabados, por lo que todas las señales se evalúan al cierre de la última vela terminada. Conjuntos de parámetros separados controlan los filtros alcistas y bajistas, lo que permite un suavizado asimétrico o longitudes de período. Los niveles de protección se crean con órdenes stop y límite nativas colocadas a `StopLossPips * PipSize` del precio de entrada. La toma de ganancias reutiliza la misma distancia de parada y la multiplica por el factor porcentual definido para cada lado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Los filtros de tendencia rápida y lenta para el lado largo están subiendo en la barra anterior (`MA_fast1[1] > MA_slow1[1]` y `MA_fast2[1] > MA_slow2[1]`) y la vela anterior cierra por encima de su promedio dedicado mientras que la vela de hace dos barras estaba por debajo (`Close[2] <= MA_cross[2]` y `Close[1] > MA_cross[1]`).
  - **Corto**: Los filtros de tendencia rápida y lenta para el lado corto están bajando en la barra anterior (`MA_fast1[1] < MA_slow1[1]` y `MA_fast2[1] < MA_slow2[1]`) y la vela anterior cierra por debajo de su promedio dedicado mientras que la vela de hace dos barras estaba por encima de ella (`Close[2] >= MA_cross[2]` y `Close[1] < MA_cross[1]`).
- **Largo/Corto**: Ambas direcciones, pero nunca simultáneamente.
- **Criterios de salida**:
  - Las posiciones se cierran exclusivamente mediante órdenes stop-loss de protección o órdenes take-profit.
- **Para**: Sí. El stop se sitúa a `StopLossPips * PipSize` del precio de entrada; la toma de ganancias es igual a la distancia de parada multiplicada por el parámetro `% of SL`.
- **Valores predeterminados**:
  - `FirstTrendFastPeriod` = 5, `FirstTrendFastMethod` = SMA.
  - `FirstTrendSlowPeriod` = 20, `FirstTrendSlowMethod` = SMA.
  - `SecondTrendFastPeriod` = 20, `SecondTrendFastMethod` = SMA.
  - `SecondTrendSlowPeriod` = 30, `SecondTrendSlowMethod` = SMA.
  - `BullCrossPeriod` = 5, `BullCrossMethod` = SMA.
  - `BuyVolume` = 0,01, `BuyStopLossPips` = 50, `BuyTakeProfitPercent` = 100.
  - `FirstTrendBearFastPeriod` = 5, `FirstTrendBearFastMethod` = SMA.
  - `FirstTrendBearSlowPeriod` = 20, `FirstTrendBearSlowMethod` = SMA.
  - `SecondTrendBearFastPeriod` = 20, `SecondTrendBearFastMethod` = SMA.
  - `SecondTrendBearSlowPeriod` = 30, `SecondTrendBearSlowMethod` = SMA.
  - `BearCrossPeriod` = 5, `BearCrossMethod` = SMA.
  - `SellVolume` = 0,01, `SellStopLossPips` = 50, `SellTakeProfitPercent` = 100.
  - `PipSize` = 0,0001.
- **Filtros**:
  - Categoría: Seguimiento de tendencias.
  - Dirección: Dual (larga + corta).
  - Indicadores: Múltiples medias móviles.
  - Stops: Stop fijo basado en pips y toma de ganancias proporcional.
  - Complejidad: Moderada.
  - Plazo: funciona en la serie de velas configuradas (predeterminado 15 minutos).
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
