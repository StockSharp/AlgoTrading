# Misión Imposible Poder Dos Estrategia Abierta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una versión StockSharp del asesor experto MetaTrader “Mission Impossible Power Two Open”. Supervisa la dirección de la vela completada más recientemente y abre una nueva cesta de operaciones en esa dirección. Cuando el precio se mueve contra la cesta activa, la estrategia agrega nuevas entradas promedio de acuerdo con una cuadrícula de pips fija. El volumen de cada nueva entrada crece con la pérdida flotante de la cesta, imitando la regla de tamaño basada en `power` del EA original. Los objetivos de salida se recalculan después de cada llenado para que toda la cesta comparta un único nivel de toma de ganancias y límite de pérdidas.

## Lógica de trading

1. **Detección de señal**: en cada vela terminada, la estrategia compara el cierre de la vela anterior con su apertura.
   - Si la vela anterior cerró por encima de su apertura, la señal larga está activa.
   - Si cerró por debajo de la apertura, la señal corta está activa.
   - Una barra interior (cerca igual a abierta) no produce ninguna canasta nueva.
2. **Apertura de la primera operación**: si no hay ninguna cuadrícula activa en la dirección indicada, la estrategia coloca una orden de mercado con el tamaño `BaseVolume`.
3. **Cuadrícula de promedio**: cuando existe una canasta, la estrategia sigue midiendo la distancia entre el último precio completado y el cierre actual.
   - Para largos, se agrega una nueva entrada una vez que el precio cae al menos `GridStepPips * PriceStep` por debajo del último llenado.
   - Para los cortos, la estrategia espera hasta que el precio suba la misma distancia por encima del último relleno.
   - La cuadrícula deja de agregar nuevas posiciones después de que se hayan alcanzado `MaxTrades` rellenos en la dirección respectiva.
4. **Volumen dinámico**: antes de enviar cada nuevo pedido, la estrategia calcula la pérdida no realizada de la cesta, la multiplica por `Power * 0.0001` y suma el resultado a `BaseVolume`. El tamaño final se redondea al paso del volumen de intercambio, se fija entre los límites de seguridad y se limita a `MaxVolume`.
5. **Gestión de salida**: después de cada llenado, la estrategia vuelve a calcular los objetivos compartidos para toda la cesta:
   - Con una sola posición, la toma de ganancias está a `TakeProfitFirstPips` de la entrada y el stop-loss está a `StopLossPips` de distancia en la dirección opuesta.
   - Con dos o más posiciones ambos niveles están anclados al precio promedio ponderado por volumen de la canasta, utilizando `TakeProfitNextPips` para la distancia objetivo y `StopLossPips` para protección.
   - Cuando el precio toca el take-profit o el stop-loss, todas las posiciones en esa dirección se cierran en el mercado.
6. **Cestas independientes**: las cuadrículas largas y cortas se rastrean de forma independiente. La estrategia puede mantener ambos al mismo tiempo cuando llegan señales alternas.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.01` | Tamaño del pedido inicial para una cesta nueva antes del escalado. |
| `MaxVolume` | `decimal` | `2` | Límite máximo para una orden de mercado único después del redondeo. |
| `Power` | `decimal` | `13` | Multiplicador aplicado a la pérdida flotante al calcular el volumen aditivo para nuevas entradas. |
| `StopLossPips` | `int` | `400` | Distancia en pasos de precio utilizados para el stop-loss compartido. |
| `TakeProfitFirstPips` | `int` | `15` | Distancia de obtención de beneficios para la primera entrada en una cesta. |
| `TakeProfitNextPips` | `int` | `7` | Distancia de obtención de beneficios para cestas promediadas (dos o más entradas). |
| `GridStepPips` | `int` | `21` | Movimiento adverso mínimo (en pasos de precio) antes de que se permita otra entrada promedio. |
| `MaxTrades` | `int` | `16` | Número máximo de operaciones de cuadrícula por dirección. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Velas utilizadas para generación de señales y gestión de cestas. |

## Notas

- Los volúmenes de órdenes siempre están alineados con el `VolumeStep` del instrumento, restringidos por el `MinVolume` y `MaxVolume` del valor siempre que esos límites estén disponibles en el tablero de negociación.
- Las máquinas de estados largos y cortos están completamente separadas, lo que permite que la estrategia mantenga cestas cubiertas cuando la dirección del mercado cambia rápidamente.
- Los niveles de protección se recalculan en cada llenado y se redondean al `PriceStep` más cercano, coincidiendo con la frecuente rutina de modificación de toma de ganancias realizada en la versión MetaTrader.
- No se utilizan buffers de indicador; Todas las decisiones se basan en datos de velas sin procesar e información de cartera, tal como en la fuente EA.
