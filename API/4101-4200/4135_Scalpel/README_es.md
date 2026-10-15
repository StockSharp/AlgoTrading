# Estrategia de bisturí
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Bisturí** es una StockSharp versión del MetaTrader 4 asesores expertos `Scalpel.mq4`. El sistema busca rupturas de impulso en el período de tiempo base, confirma el movimiento con mínimos/máximos de períodos de tiempo más altos y filtra las entradas utilizando un estudio de volumen direccional basado en velas de 1 minuto. La gestión de posiciones refleja el EA original: las ganancias se obtienen con una toma de ganancias fija que se reduce con el tiempo, el stop-loss puede seguirse una vez que el precio se ha movido a favor de la operación y cada posición se puede cerrar forzosamente después de un tiempo de vida configurable o el viernes por la noche.

## Lógica de trading
- **Filtro de tendencia de múltiples marcos temporales**: una señal larga requiere que los mínimos actuales de las velas H4, H1 y M30 sean más altos que sus mínimos anteriores. Las señales cortas exigen máximos más bajos en los mismos plazos.
- **Confirmación de ruptura**: la estrategia espera a que la mejor oferta supere el máximo anterior (largo) o que la mejor oferta caiga por debajo del mínimo anterior (corto) en el marco de tiempo base. Además, los tres máximos (o mínimos) anteriores deben formar una escalera en la dirección de ruptura.
- Ventana **CCI**: el índice del canal de productos básicos de la vela cerrada anterior debe permanecer dentro de una banda configurable alrededor de cero. Los límites positivos utilizan una ventana simétrica; Los límites negativos relajan el requisito para uno de los lados exactamente como en el EA original.
- **Filtro de volumen direccional**: los volúmenes del período de volatilidad se dividen en dos bloques móviles. Se permite una operación solo si el bloque más reciente muestra más volumen direccional que el bloque más antiguo y el bloque más antiguo es distinto de cero. Los valores negativos `VolatilityWindow` cambian el filtro a acumulación basada en rango (no direccional).
- **Gestión de riesgos**:
  - Distancias fijas de toma de ganancias y stop loss expresadas en pasos de precio mínimo.
  - El nivel de obtención de beneficios se reduce en un paso de precio cada `TakeProfitReduceMinutes` minutos que la posición permanece abierta.
  - Un trailing stop se activa después de que el precio se ha movido `TrailingStopPoints` y luego sigue el movimiento vela por vela.
  - Las posiciones se pueden cerrar forzosamente después de `LiveMinutes` o en el `FridayCloseHour` configurado.
  - Las nuevas entradas se bloquean mientras la posición neta absoluta sea igual a `MaxDirectionalPositions * TradeVolume` y, opcionalmente, mientras el tiempo de reutilización de reingreso esté activo.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | `-5` | Tamaño del pedido. Los valores positivos utilizan lotes fijos; los valores negativos representan un porcentaje del capital de la cartera convertido en volumen utilizando el precio de venta actual. |
| `TakeProfitPoints` | `40` | Distancia desde la entrada hasta el objetivo de obtención de beneficios en pasos de precio. |
| `StopLossPoints` | `340` | Distancia desde la entrada hasta el stop-loss en pasos de precio. |
| `TrailingStopPoints` | `25` | Distancia del trailing stop en pasos de precio. El rastro se activa una vez que el movimiento excede esta distancia. |
| `CciPeriod` | `14` | Período retroactivo para el índice del canal de productos básicos calculado sobre el período base. |
| `CciLimit` | `75` | Límite superior para entradas largas y límite negativo reflejado para entradas cortas. Los valores negativos reproducen los límites asimétricos del EA original. |
| `MaxDirectionalPositions` | `1` | Unidades de posición neta máximas (en múltiplos del volumen comercial calculado) permitidas en una dirección. |
| `ReentryIntervalMinutes` | `0` | Número mínimo de minutos de espera entre dos entradas consecutivas. |
| `TakeProfitReduceMinutes` | `600` | Minutos antes de que el umbral de toma de ganancias se reduzca en un paso de precio. Establezca en `0` para desactivar la reducción. |
| `LiveMinutes` | `0` | Vida útil máxima de una posición en minutos. Un valor de `0` desactiva el temporizador. |
| `VolatilityWindow` | `100` | Número de velas de volatilidad almacenadas en cada bloque rodante. Los valores negativos cambian a acumulación basada en rango, `0` usa solo la última vela. |
| `VolatilityThresholdPoints` | `1` | Cuerpo mínimo de vela (ventana positiva) o rango (ventana no direccional) requerido para acumular volumen. El letrero invierte la interpretación de subir/bajar volúmenes. |
| `FridayCloseHour` | `22` | Hora del día (0-23) utilizada para liquidar posiciones los viernes por la noche. `0` desactiva la salida del viernes. |
| `SpreadLimitPoints` | `5.5` | Spread máximo permitido en pasos de precio al abrir una nueva posición. |
| `CandleType` | `1 minute` | Cronograma base que genera entradas y gestiona salidas. |
| `Hour1CandleType` | `1 hour` | Se utilizó un período de tiempo más alto para la confirmación de la tendencia del primer semestre. |
| `Hour4CandleType` | `4 hours` | Se utilizó un período de tiempo más alto para la confirmación de la tendencia del cuarto semestre. |
| `Minute30CandleType` | `30 minutes` | Marco de tiempo más alto utilizado para la confirmación de la tendencia M30. |
| `VolatilityCandleType` | `1 minute` | Periodo de tiempo que alimenta el filtro de volumen direccional. |

## Notas de implementación
- La estrategia se suscribe al libro de órdenes para reutilizar los mejores precios de oferta y demanda más recientes para la detección de rupturas y el filtrado de diferenciales.
- Todos los enlaces de indicadores se basan en el nivel alto API de StockSharp: el valor de CCI se obtiene a través de `BindEx`, mientras que los períodos de tiempo más altos utilizan suscripciones dedicadas.
- Las paradas dinámicas y las reducciones de toma de ganancias se ejecutan en código en lugar de mediante órdenes de protección para imitar el comportamiento original de EA.
- Los valores negativos de `TradeVolume` dependen del precio de venta actual y de las restricciones de volumen de seguridad. Cuando el tamaño calculado cae por debajo del lote mínimo, se redondea automáticamente hacia arriba.

## Uso
1. Adjunte la estrategia a una cartera y elija el valor deseado.
2. Configure los parámetros de plazo, los umbrales de riesgo y las reglas de tamaño de volumen.
3. Inicia la estrategia. Las señales se evalúan únicamente en velas terminadas; las posiciones se abren con órdenes de mercado y se cierran mediante las reglas de gestión de riesgos integradas.
