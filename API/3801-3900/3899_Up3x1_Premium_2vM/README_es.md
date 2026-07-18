# Estrategia Up3x1 Premium 2vM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia es una adaptación directa del asesor experto MetaTrader 4 *up3x1_Premium_2vM*. Opera con un solo símbolo y mantiene como máximo una posición abierta en cualquier momento. Las entradas se basan en una combinación de promedios móviles suavizados, fuertes rangos de velas y un filtro de ruptura diario a medianoche. El riesgo se gestiona a través de distancias fijas de toma de ganancias y stop-loss expresadas en puntos de precio, mientras que un trailing stop opcional reproduce el comportamiento del EA original que ajusta continuamente los stop una vez que el mercado se mueve a favor de la posición.

## como funciona

1. El período de tiempo principal es configurable; el EA utilizó originalmente el período de tiempo del gráfico. Dos medias móviles suavizadas (SMMA) con períodos 12 y 26 están vinculadas a la suscripción de velas utilizando el precio típico.
2. Un flujo de velas diario separado reconstruye los datos D1 utilizados por la lógica MQL para el filtro de ruptura de medianoche y para el promedio móvil simple diario de 10 períodos.
3. Cuando es plana, la estrategia evalúa las dos velas terminadas anteriores y los valores SMMA almacenados en caché:
   - **Sesgo largo**: o la SMMA rápida cruza por encima de la SMMA lenta mientras ambas aperturas aumentan, o la última vela muestra un cuerpo alcista por encima de los umbrales del rango configurado, o la última vela diaria cierra alcista después de un rango grande. El EA original también comparó el SMA diario con el precio de venta; debido a que la condición siempre se evalúa como verdadera, se conserva por motivos de compatibilidad.
   - **Sesgo corto**: condiciones simétricas de las reglas largas utilizando rangos y cruces bajistas.
   - Si se cumple alguna condición larga, se emite una compra de mercado; de lo contrario, si se mantiene alguna condición corta, se realiza una venta en el mercado. El tamaño del lote solicitado se normaliza según el paso del volumen de seguridad antes de enviar el pedido.
4. Mientras una posición está abierta, la estrategia monitorea los valores SMMA rápido/lento de la vela anterior. Cuando su diferencia absoluta cae por debajo de `ConvergenceTolerance` la posición se cierra, reproduciéndose la comprobación de igualdad en el asesor experto.
5. El módulo de seguimiento rastrea el precio de entrada promedio. Una vez que el precio supera la distancia de seguimiento, el nivel de parada avanza para mantener la brecha configurada. Al tocar ese nivel, la posición se cierra inmediatamente, emulando las repetidas llamadas de `OrderModify` de MQL.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | `TimeFrame(1h)` | Plazo principal utilizado para las entradas. |
| `FastMaPeriod` | `12` | Longitud de la media móvil suavizada rápida (precio típico). |
| `SlowMaPeriod` | `26` | Longitud de la media móvil suavizada lenta (precio típico). |
| `RangeThreshold` | `0.0060` | Rango de vela mínimo requerido por el filtro de impulso. |
| `BodyThreshold` | `0.0050` | Tamaño mínimo del cuerpo de la vela para la condición del rango. |
| `DailyRangeThreshold` | `0.0060` | Distancia mínima de apertura y cierre en la última vela diaria para el filtro de ruptura de medianoche. |
| `TakeProfitPoints` | `150` | Distancia de obtención de beneficios expresada en puntos de precio. Establezca en `0` para desactivar. |
| `StopLossPoints` | `100` | Distancia de stop-loss expresada en puntos de precio. Establezca en `0` para desactivar. |
| `TrailingStopPoints` | `10` | Distancia entre el precio y el trailing stop. Establezca en `0` para deshabilitar el seguimiento. |
| `TradeVolume` | `0.05` | Tamaño de lote utilizado para órdenes de mercado antes de la normalización del volumen. |
| `ConvergenceTolerance` | `0.00001` | Diferencia máxima entre las SMMA que desencadena la liquidación de posiciones. |

## Notas

- La estrategia mantiene la peculiaridad original de EA donde la comparación diaria de SMA siempre es cierta, lo que garantiza la paridad de características con la fuente de MQL.
- Las órdenes de stop-loss y take-profit se registran a través de `StartProtection` y, por lo tanto, se adaptan automáticamente al tamaño del paso del corredor cuando esté disponible.
- La lógica final requiere un valor `TrailingStopPoints` positivo y un `Security.PriceStep` válido. Cuando falta alguno de los datos, la parada no seguirá.
- La normalización del volumen respeta las restricciones de intercambio (`VolumeStep`, `VolumeMin`, `VolumeMax`). Los valores negativos para `TradeVolume` se pueden usar para emular el tamaño basado en porcentaje una vez que se agrega la lógica personalizada.
