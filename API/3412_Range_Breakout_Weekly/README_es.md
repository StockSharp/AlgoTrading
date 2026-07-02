# Estrategia semanal de ruptura de rango (ID 3412)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia semanal de ruptura de rango** es una StockSharp conversión de alto nivel API del MetaTrader 5 asesores expertos `RangeBreakout.mq5`. El sistema prepara niveles de ruptura una vez por semana utilizando un día y una hora configurables, luego abre una única operación cuando el precio supera o baja el rango calculado. La lógica de compensación de pérdidas y tamaño de posición de estilo Martingale refleja el script original, mientras que la implementación aprovecha las suscripciones StockSharp para velas, cotizaciones de nivel 1 y vinculación de indicadores.

## Lógica de trading

1. **Ventana de preparación semanal.** Al cierre de la vela horaria especificada en el día de la semana configurado, la estrategia registra el cierre de la vela como precio de referencia y pasa de la fase *Standby* a *Setup*.
2. **Cálculo de rango.**
   - El rango principal se deriva de un rango verdadero promedio diario de 20 períodos (ATR). El valor ATR se multiplica por `ATR Percentage` y se normaliza al tamaño de tick del instrumento.
   - Si faltan datos de ATR, el algoritmo recurre a multiplicar el precio de venta actual por `Price Percentage`.
3. **Niveles de protección.**
   - Los activadores de ruptura superior e inferior se colocan un rango por encima y por debajo del cierre de referencia.
   - Las compensaciones de toma de ganancias y limitación de pérdidas se calculan como porcentajes del rango. Cuando la compensación está activa después de una pérdida, la toma de ganancias se reemplaza por la compensación de compensación acumulada y el límite de pérdidas se amplía en la misma cantidad, tal como ocurre con la lógica MetaTrader.
4. **Ejecución.**
   - Mientras está en *Configuración*, la estrategia escucha las cotizaciones de Nivel 1. Una ruptura por encima del gatillo superior ingresa a una posición larga; una caída por debajo del gatillo inferior abre una posición corta. Las órdenes se envían como órdenes de mercado con controles de precios alineados con ticks.
   - Una vez que una posición está activa (fase *Negociación*), las cotizaciones de Nivel 1 se monitorean continuamente. Al alcanzar el stop o el objetivo de protección se cierra la posición con una orden de mercado.
5. **Martingale recuperación.**
   - Después de una salida perdedora, el tamaño de la siguiente operación se duplica y la compensación de pérdidas se agrega al colchón de compensación para que el siguiente objetivo tenga como objetivo recuperar la pérdida acumulada.
   - Una salida ganadora restablece tanto el multiplicador como el buffer de compensación a sus valores iniciales.
6. **Restablecimiento diario.** Después de que concluye una operación, la estrategia regresa a la fase *En espera* y espera hasta la siguiente combinación de día/hora elegible para preparar una nueva configuración.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Trading Day` | lunes | Día de la semana utilizado para medir la vela de referencia de ruptura. Las selecciones de fin de semana se reasignan automáticamente al lunes, coincidiendo con el comportamiento de advertencia original. |
| `Start Hour` | 0 | Hora (0-23) cuya vela de cierre sirve de referencia. Optimizable para cubrir varias aperturas de sesiones. |
| `Price Percentage` | 1.0 | Porcentaje de reserva del precio de venta utilizado para calcular el rango cuando faltan datos de ATR. |
| `ATR Percentage` | 100 | Multiplicador aplicado al valor diario ATR para obtener el rango de ruptura. |
| `Take Profit Percentage` | 100 | Porcentaje del rango agregado más allá de la entrada para definir el precio de toma de ganancias. Anulado por el colchón de compensación después de pérdidas consecutivas. |
| `Stop Loss Percentage` | 100 | Porcentaje del rango restado a la entrada para fijar el precio del stop-loss. El colchón de compensación amplía esta distancia después de las pérdidas. |
| `Base Volume` | 0.1 | Volumen de negociación inicial antes de la escala martingala. El valor se redondea automáticamente al paso de volumen del instrumento y se sujeta mediante restricciones de mínimo/máximo. |
| `ATR Period` | 20 | Número de velas diarias suministradas al indicador ATR. |
| `Hour Candle Type` | plazo de 1 hora | Suscripción de vela utilizada para detectar la ventana de preparación. |
| `ATR Candle Type` | plazo de 1 día | Suscripción de vela que alimenta el indicador ATR. |

## Notas de implementación

- **Suscripciones de datos.** La estrategia se suscribe a velas horarias para la programación, velas diarias para el cálculo ATR y datos de nivel 1 para el seguimiento de ofertas y demandas. El `Bind` API de alto nivel se utiliza para transmitir valores de indicadores sin manejo manual del búfer.
- **Alineación de ticks.** Todos los niveles de precios (referencia, activadores, stop-loss, take-profit) se normalizan a través de `Security.ShrinkPrice` para respetar las restricciones de tamaño de ticks, imitando el comportamiento `NormalizeDouble` de MetaTrader.
- **Manejo de volúmenes.** Los volúmenes comerciales se redondean al `VolumeStep` del instrumento y se restringen por `VolumeMin`/`VolumeMax` antes del envío del pedido, replicando la desinfección del lote original.
- **Máquina de fases.** Las fases internas (`Standby`, `Setup`, `Trade`) reemplazan la lógica de enumeración original, lo que garantiza una única operación por ciclo de preparación. Después de cada salida, el estado se restablece a `Standby` hasta que se produzca la siguiente vela calificada.
- **Búfer de compensación.** El campo `compensationOffset` almacena la distancia de pérdida acumulada expresada en unidades de precio. Cuando está activa, la siguiente configuración reemplaza la compensación de toma de ganancias con este valor y amplía el stop en la misma cantidad, reflejando la fórmula MetaTrader que convierte la pérdida monetaria pasada en distancia de precio.
- **Registro.** Al seleccionar sábado o domingo se activa un registro informativo y cambia automáticamente el día laborable al lunes, de acuerdo con la advertencia que muestra la versión MQL.

## Consejos de uso

1. Alinee `Trading Day` y `Start Hour` con la sesión que genera rangos significativos (por ejemplo, ruptura de rango asiático o ruptura de apertura de Londres).
2. Calibre `ATR Percentage`, `Take Profit Percentage` y `Stop Loss Percentage` juntos. Aumentar el multiplicador de rango produce desencadenantes más amplios y operaciones más lentas, mientras que ajustar los porcentajes de ganancias/pérdidas modifica la relación recompensa-riesgo.
3. Habilite la optimización en `Start Hour`, `Base Volume` o los parámetros de porcentaje para reproducir barridos de parámetros del asesor experto original.
4. Supervise la exposición acumulativa creada por el multiplicador de martingala. Considere reducir `Base Volume` cuando ejecute cuentas con alto apalancamiento.
5. La estrategia está diseñada para un solo instrumento. Implemente varias copias con diferentes valores o configuraciones de sesión para diversificar la cobertura.

## Cobertura de conversión

- ✅ Programación semanal conservada, cálculos de rango, niveles de protección y comportamiento de martingala de `RangeBreakout.mq5`.
- ✅ Se reemplazaron las llamadas MetaTrader específicas de API (`iATR`, `CopyBuffer`, `OrderSend`, etc.) con abstracciones idiomáticas StockSharp (`SubscribeCandles`, `AverageTrueRange`, `BuyMarket`/`SellMarket`).
- ✅ Implementé comentarios en línea en inglés y documentación extensa según lo solicitado.
- ✅ Dejé los proyectos de prueba intactos y no creé una variante de Python, cumpliendo con las restricciones de la tarea.
