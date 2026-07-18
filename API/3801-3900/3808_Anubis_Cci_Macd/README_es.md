# Estrategia de Anubis CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Convierte el MetaTrader 4 asesor experto "Anubis" al StockSharp alto nivel API.
- Utiliza un filtro de índice de canales de productos básicos (CCI) de 4 horas junto con un cruce de MACD de 15 minutos.
- Aplica un tamaño de posición adaptable, stop-loss, protección de equilibrio, salidas impulsadas por ATR y una toma de ganancias basada en la desviación estándar.

## Lógica estratégica
1. **Datos**
   - Periodo de tiempo principal: velas de 15 minutos (`SignalCandleType`), utilizadas para los cálculos MACD y ATR.
   - Plazo superior: velas de 4 horas (`TrendCandleType`), utilizadas para el filtrado CCI y la medición de la desviación estándar.
2. **Indicadores**
   - `CommodityChannelIndex` con periodo configurable en la serie 4H.
   - `StandardDeviation` (longitud 30) en 4H cierra para estimar la distancia de obtención de beneficios.
   - `MovingAverageConvergenceDivergenceSignal` (rápido/lento/señal configurable) en 15 millones de velas.
   - `AverageTrueRange` (longitud 12) en velas de 15 millones para salidas basadas en volatilidad.
3. **Inscripciones**
   - **Corto**: cuando 4H CCI está por encima de `CciThreshold`, los dos valores anteriores de MACD muestran un cruce bajista (MACD cruza por debajo de su señal), MACD fue positivo, no hay posiciones largas abiertas y el precio se ha movido al menos `PriceFilterPoints` desde la última entrada corta.
   - **Largo**: condición simétrica con CCI debajo de `-CciThreshold`, MACD cruzando hacia arriba mientras es negativo, sin cortos abiertos y con el filtro de distancia mínima satisfecho.
4. **Gestión de riesgos**
   - El volumen base se define por `VolumeValue` y se escala según el capital de la cuenta (2× por encima de 14k, 3,2× por encima de 22k) y por `LossFactor` después de una operación perdedora.
   - Las operaciones simultáneas máximas por dirección están limitadas por `MaxLongTrades` y `MaxShortTrades`.
   - Stop-loss duro colocado prácticamente a `StopLossPoints * PriceStep` del precio medio de entrada.
   - El punto de equilibrio se activa una vez que el precio avanza `BreakevenPoints` y cierra inmediatamente la posición si el precio vuelve a la entrada.
5. **Sale**
   - La toma de ganancias con desviación estándar cierra la posición una vez que el precio se mueve `StdDevMultiplier * StdDev` a favor.
   - Las salidas agresivas se activan cuando el rango de la vela anterior excede `CloseAtrMultiplier * ATR`.
   - Las salidas de desaceleración de MACD requieren tanto una ganancia suficiente (`ProfitThresholdPoints`) como una reversión en la pendiente MACD ({PH003}} anterior menor o mayor que hace dos barras, dependiendo de la dirección).
   - El stop de protección cierra la operación si el precio supera la distancia del stop-loss o vuelve a caer hasta la entrada después de la activación del punto de equilibrio.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `VolumeValue` | Volumen base de pedidos. |
| `CciThreshold` | Umbral absoluto para el filtro 4H CCI. |
| `CciPeriod` | Período del indicador 4H CCI. |
| `StopLossPoints` | Distancia de stop-loss en puntos. |
| `BreakevenPoints` | Ganancia en puntos necesarios para armar el punto de equilibrio. |
| `MacdFastPeriod` | Período rápido de EMA para MACD. |
| `MacdSlowPeriod` | Período lento de EMA durante MACD. |
| `MacdSignalPeriod` | Periodo de señal EMA durante MACD. |
| `LossFactor` | Multiplicador de volumen aplicado después de una operación perdedora. |
| `MaxShortTrades` | Número máximo de entradas cortas simultáneas. |
| `MaxLongTrades` | Número máximo de entradas largas simultáneas. |
| `CloseAtrMultiplier` | multiplicador ATR para salidas anticipadas. |
| `ProfitThresholdPoints` | Búfer de ganancias adicional (puntos) antes de que salga MACD. |
| `StdDevMultiplier` | Multiplicador de desviación estándar para la toma de ganancias. |
| `PriceFilterPoints` | Movimiento mínimo del precio entre entradas consecutivas. |
| `SignalCandleType` | Periodo de tiempo principal para MACD y ATR. |
| `TrendCandleType` | Plazo mayor para CCI y desviación estándar. |

## Notas
- La estrategia se basa en metadatos `Security.PriceStep` válidos para traducir parámetros basados en puntos en distancias de precios.
- La lógica de protección se implementa mediante controles explícitos en lugar de órdenes de límite/detención pendientes, lo que refleja el comportamiento original de EA con paradas virtuales.
- La versión de Python se omite intencionalmente según las instrucciones de la tarea.
