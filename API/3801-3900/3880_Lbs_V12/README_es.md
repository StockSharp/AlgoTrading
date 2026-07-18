# Estrategia Lbs V12
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Lbs V12 es una conversión del asesor experto MetaTrader **LBS_V12.mq4**. Abre un par de órdenes de parada de ruptura alrededor de la vela anterior de 15 minutos cuando comienza la hora de activación configurada. Ambas órdenes se compensan con el valor actual del rango verdadero promedio (ATR) para tener en cuenta la volatilidad a corto plazo. La estrategia intenta captar el primer impulso de la sesión de negociación y gestiona las salidas mediante reglas virtuales de stop-loss, take-profit y trailing evaluadas en cada vela terminada.

## Lógica de trading
1. La estrategia monitorea las velas terminadas del período de tiempo seleccionado (15 minutos por defecto).
2. Cuando aparece una nueva vela con el minuto `00` en el `TriggerHour` configurado, la vela anterior se convierte en el rango de referencia.
3. Si no hay posiciones abiertas ni órdenes de trabajo para el día actual, se envían dos órdenes de parada:
   - **Stop de compra** por encima del máximo de referencia más el diferencial del instrumento, un paso de precio y el último valor ATR.
   - **Parada de venta** por debajo del mínimo de referencia menos los mismos amortiguadores.
4. Los niveles de precios de protección para cada lado se almacenan internamente:
   - El stop-loss se coloca más allá del extremo opuesto de la vela de referencia.
   - La toma de ganancias se calcula utilizando la distancia de puntos estilo MetaTrader.
   - Un trailing stop se activa una vez que la operación se mueve más allá de la distancia configurada.
5. Cuando se abre una posición larga o corta, se cancela la orden stop opuesta. Toda la protección se aplica virtualmente: los máximos y mínimos de las velas se comparan con los valores de stop/take almacenados y la posición se cierra con órdenes de mercado cuando se alcanzan los límites.
6. La estrategia se ejecuta solo una vez al día. Todas las órdenes pendientes y el estado interno se borran al comienzo de una nueva fecha de negociación.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `Volume` | Volumen de negociación en lotes. | `1` |
| `TriggerHour` | Hora del día (zona horaria de la terminal) en la que se deben enviar las órdenes de ruptura. | `9` |
| `TakeProfitPoints` | Puntos de estilo MetaTrader entre el precio de entrada y el objetivo de obtención de beneficios. | `100` |
| `TrailingStopPoints` | Puntos de estilo MetaTrader utilizados para el trailing stop después de que la operación genera ganancias. | `20` |
| `AtrPeriod` | Periodo del indicador ATR que compensa las órdenes pendientes. | `3` |
| `CandleType` | Tipo de vela utilizado para cálculos de señales. El valor predeterminado son velas con un marco de tiempo de 15 minutos. | `15m timeframe` |

## Gestión del riesgo
- Las salidas se ejecutan a través de órdenes de mercado cuando los extremos de las velas tocan los niveles virtuales de stop-loss o take-profit.
- El trailing stop aumenta (para largos) o disminuye (para cortos) el nivel de protección siempre que la operación gana más que la distancia configurada.
- El reinicio diario garantiza que la estrategia no acumule múltiples posiciones u órdenes pendientes obsoletas.

## Notas
- Las actualizaciones precisas de oferta y demanda mejoran la compensación del diferencial que se agrega a los precios de ruptura. Si no se dispone de datos sobre diferenciales, la estrategia retrocede a un escalón de precio.
- La conversión mantiene los valores predeterminados originales de MetaTrader pero adapta el manejo de la obtención de ganancias para posiciones cortas de modo que el objetivo siempre se coloque en la dirección rentable.
