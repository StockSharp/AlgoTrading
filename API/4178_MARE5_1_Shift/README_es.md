# MARE5.1 Estrategia de cruce de turnos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MARE5.1 es una adaptación de C# del asesor experto MetaTrader 4 `MARE5_1.mq4`. El robot original operaba con datos M1 y se basaba en un par de promedios móviles simples evaluados en tres compensaciones históricas diferentes para detectar cambios de régimen. Esta implementación StockSharp reproduce el comportamiento con parámetros configurables, adjunta órdenes de protección al estilo MetaTrader y expone un filtro de ventana de negociación detallado.

## Lógica de trading
1. **Datos de mercado**
   - Una suscripción de vela única definida por `CandleType` (predeterminado: 1 minuto) alimenta los cálculos.
   - Cada vela se procesa sólo después de cerrarse para evitar el uso de barras a medio formar.
2. **Indicadores**
   - Dos instancias `SimpleMovingAverage` representan los componentes rápido (`FastPeriod`) y lento (`SlowPeriod`).
   - Ambos promedios se desplazan hacia adelante en `MovingAverageShift`, exactamente como el argumento `ma_shift` en la función MQL `iMA`.
   - Se obtienen copias retrasadas adicionales de cada promedio con turnos de `MovingAverageShift + 2` y `MovingAverageShift + 5` para reflejar las llamadas originales de `iMA(..., shift=2/5)`.
3. **Detección de señal**
   - La diferencia entre los promedios debe exceder al menos un paso de precio (`Point` en términos de MetaTrader). Si el instrumento tiene cero `PriceStep`, cualquier diferencia positiva es suficiente.
   - **Vender configuración:**
     - La vela anterior debe ser bajista (`Close < Open`).
     - El promedio lento desplazado actual es mayor que el promedio rápido.
     - Dos y cinco velas atrás, el promedio rápido todavía estaba por encima del promedio lento, lo que indica un cambio de impulso.
   - **Comprar configuración:**
     - La vela anterior debe ser alcista (`Close > Open`).
     - El promedio rápido desplazado actual es mayor que el promedio lento.
     - Dos y cinco velas atrás, el promedio lento seguía liderando, confirmando una transición de condiciones bajistas a alcistas.
   - Sólo puede haber una posición abierta a la vez, replicando la guardia `OrdersTotal() < 1` del EA.
4. **Filtro de tiempo**
   - Solo se permite operar cuando la hora de cierre de la vela evaluada cae dentro del intervalo `[TimeOpenHour, TimeCloseHour]`.
   - Si la hora de finalización es menor que la hora de inicio, la ventana se considera durante la noche (por ejemplo, de `22` a `5`).

## Gestión del riesgo
- `StartProtection` está configurado con una distancia de stop-loss y take-profit convertida de MetaTrader puntos en compensaciones de precio absoluto utilizando el instrumento `PriceStep`.
- No se implementa ningún trailing stop porque el código original declaraba `TrailingStop` pero nunca lo usó.
- Los pedidos se envían con el volumen definido por `TradeVolume`. La estrategia no piramidal ni escala posiciones.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `TradeVolume` | Tamaño del lote para entradas al mercado. | `7.8` | Redondeado según reglas de intercambio por el conector StockSharp. |
| `FastPeriod` | Período de la media móvil simple rápida. | `13` | Controla la rapidez con la que reacciona la estrategia a los cambios de precios. |
| `SlowPeriod` | Período de la media móvil simple lenta. | `55` | Proporciona la referencia de tendencia a largo plazo. |
| `MovingAverageShift` | Desplazamiento hacia adelante aplicado a ambas medias móviles. | `2` | Coincide con el parámetro `ma_shift` de la función MQL `iMA`. |
| `StopLossPoints` | Distancia de parada de protección en MetaTrader puntos. | `80` | Convertido a un desplazamiento absoluto a través del instrumento `PriceStep`. |
| `TakeProfitPoints` | Distancia objetivo de ganancias en MetaTrader puntos. | `110` | Establezca en `0` para deshabilitar la obtención de ganancias. |
| `TimeOpenHour` | Comienzo de la ventana de negociación permitida (hora, 0–23). | `8` | Evaluado frente al tiempo de cierre de la vela. |
| `TimeCloseHour` | Fin de la ventana de negociación permitida (hora, 0 a 23). | `14` | Puede ser inferior a `TimeOpenHour` para abarcar la medianoche. |
| `CandleType` | Plazo utilizado para la suscripción de velas. | `1 minute` | Se puede proporcionar cualquier otro valor `TimeFrame()`. |

## Notas de implementación
- El indicador `Shift` se utiliza internamente para reproducir las compensaciones históricas exactas de la implementación MQL sin acceder directamente a los buffers del indicador.
- `IsDifferenceSatisfied` encapsula la comparación de umbral de puntos, manteniendo la estrategia compatible con instrumentos que tienen diferentes tamaños de ticks.
- La verificación de la ventana de negociación utiliza tiempos de cierre de velas, que es la mejor aproximación de `Hour()` de MetaTrader cuando solo se procesan velas terminadas.
- Todos los comentarios están escritos en inglés y el código se basa únicamente en el API (`SubscribeCandles().Bind(...)`) de alto nivel como lo exigen las pautas del proyecto.

## Diferencias en comparación con la versión MQL
- Las señales se evalúan en velas cerradas, lo que elimina el posible repintado que podría ocurrir en los ticks dentro de la barra en MetaTrader.
- Las órdenes de limitación de pérdidas y toma de ganancias las maneja `StartProtection` en lugar de adjuntarlas manualmente a cada llamada de `OrderSend`.
- La entrada `TrailingStop` no utilizada se omitió intencionalmente para evitar exponer un parámetro no funcional.
- El filtro de tiempo admite sesiones nocturnas por diseño, mientras que el EA original asumía implícitamente `TimeOpen <= TimeClose`.
