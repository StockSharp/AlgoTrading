# Estrategia RangeBreakout2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia RangeBreakout2** es una versión StockSharp del asesor experto MetaTrader "RangeBreakout2". El algoritmo prepara un rango de precios en momentos configurables (semanalmente, diariamente o continuamente) y abre una única orden de mercado una vez que las cotizaciones de oferta/demanda escapan de ese rango. Después de cada operación, se reinicia el ciclo de preparación del rango. La implementación reproduce las reglas originales de administración de dinero (constante, lineal, martingala y escala Fibonacci) y la expansión opcional de la distancia de obtención de ganancias después de una operación perdedora.

La estrategia funciona con un único valor y se basa en las mejores cotizaciones de oferta y demanda. Asegúrese de que el adaptador proporcione datos actualizados del libro de pedidos para que la detección de rupturas siga respondiendo.

## Lógica de trading

1. **Programación**: en el momento configurado, la estrategia registra el precio de venta actual como el centro de la configuración y deriva los niveles de ruptura superior/inferior del rango bruto.
2. **Cálculo de rango**: el rango bruto se obtiene de uno de tres modos:
   - **ATR**: multiplica el último valor del rango verdadero promedio por `AtrPercentage`.
   - **Porcentaje**: utiliza el `PricePercentage` por ciento del precio de venta actual.
   - **Fijo**: convierte `FixedRangePoints` pasos de precio en una distancia absoluta.
3. **Detección de rupturas**: mientras se encuentra en la fase `Setup`, la estrategia observa la mejor oferta/demanda. Cuando la demanda se mueve por encima del nivel superior o la oferta cae por debajo del nivel inferior, envía una orden de mercado.
4. **Tipo de entrada**: `TradeMode` selecciona entre ruptura (`Stop`), desvanecimiento (`Limit`) o comportamiento aleatorio. El modo aleatorio elige ruptura o desvanecimiento en cada entrada.
5. **Protección**: las compensaciones de stop-loss y take-profit se derivan del rango bruto. Si la operación anterior se cerró con pérdida y `RangeMultiplier` es mayor que 1, la distancia de obtención de beneficios se amplía con ese multiplicador.
6. **Gestión de dinero**: el volumen de pedidos se calcula a partir del capital libre de la cartera (`CurrentValue - BlockedValue`) y el modo de lote seleccionado:
   - **Constante** – Siempre utiliza el volumen base.
   - **Lineal**: aumenta linealmente después de cada pérdida.
   - **Martingale** – Multiplica el volumen anterior por `LotMultiplier` después de una pérdida.
   - **Fibonacci** – Crece siguiendo la secuencia Fibonacci después de las pérdidas.

Una vez que se cierra la posición, la estrategia se reinicia a la fase de espera y espera el siguiente activador del programa.

## Parámetros

| grupo | Nombre | Descripción | Predeterminado |
|-------|------|-------------|---------|
| Horario | `Periodicity` | Frecuencia de preparación de rango: Semanal, Diaria o NonStop. | `Weekly` |
| Horario | `Day` | Día de negociación utilizado cuando `Periodicity` = Semanal. | `Monday` |
| Horario | `Hour` | Hora del día en que se crea la configuración (ajuste de estilo MetaTrader: valor almacenado + 1, limitado a 0 si ≥ 23). | `0` |
| Rango | `RangeMode` | Método de cálculo del rango sin procesar (ATR/Porcentaje/Fijo). | `Atr` |
| Rango | `AtrPercentage` | Multiplicador porcentual aplicado al valor ATR. | `50` |
| Rango | `AtrLength` | Número de velas utilizadas en el indicador ATR. | `20` |
| Rango | `PricePercentage` | Porcentaje del precio de venta actual utilizado cuando `RangeMode = Percent`. | `1` |
| Rango | `FixedRangePoints` | Rango fijo expresado en incrementos de precio cuando `RangeMode = Fixed`. | `1000` |
| Comercio | `RangePercentage` | Porcentaje del rango bruto aplicado a los niveles de ruptura. | `100` |
| Comercio | `TradeMode` | Estilo de entrada: Detener (ruptura), Límite (desvanecimiento) o Aleatorio. | `Stop` |
| Comercio | `TakeProfitPercentage` | Distancia de obtención de beneficios como porcentaje del rango (opcionalmente ampliado). | `100` |
| Comercio | `StopLossPercentage` | Distancia de stop-loss como porcentaje del rango base. | `100` |
| Riesgo | `LotMode` | Esquema de gestión de lotes (Constante / Lineal / Martingale / Fibonacci). | `Martingale` |
| Riesgo | `MarginPercentage` | Porción de capital libre reservada para el volumen de pedido base. | `10` |
| Riesgo | `LotMultiplier` | Multiplicador aplicado en modos de escala tipo martingala. | `2` |
| Riesgo | `RangeMultiplier` | Multiplicador de toma de ganancias aplicado después de una operación perdedora. | `1` |
| Datos | `SignalCandleType` | Tipo de vela utilizada para comprobar las condiciones de programación. | `1m time-frame` |
| Datos | `AtrCandleType` | Tipo de vela utilizado para el cálculo de ATR. Solo se solicita cuando `RangeMode = Atr`. | `1d time-frame` |

## Notas de implementación

- La estrategia requiere actualizaciones de oferta/demanda en vivo; sin ellos, la detección de fugas no se activará.
- Los cálculos del volumen base se basan en el capital de la cartera (`CurrentValue - BlockedValue`). Cuando el conector no proporciona estos campos, el volumen vuelve al mínimo de intercambio.
- Las órdenes de protección se realizan a través de `SetStopLoss` y `SetTakeProfit`. La posición resultante (después de la nueva operación) se pasa para que la clase base pueda gestionar la protección combinada para escenarios de escalamiento.
- El respaldo ATR imita al asesor experto original: si el indicador no está listo, el rango predeterminado es el 1% del precio de venta actual.
- El modo de comercio aleatorio utiliza la clase .NET `Random` sembrada en la construcción de estrategias. Por lo tanto, dos rupturas consecutivas pueden elegir diferentes tipos de entrada.

## Consejos de uso

1. Configure el `SignalCandleType` para que coincida con la resolución deseada de las comprobaciones programadas. Un flujo de velas de un minuto reproduce fielmente el comportamiento impulsado por ticks de la versión MQL.
2. Para programaciones semanales, asegúrese de que la zona horaria del servidor coincida con las expectativas del EA original.
3. Supervise el efecto de `RangeMultiplier` cuando utilice modos de lote tipo martingala: ampliar la distancia de obtención de beneficios junto con volúmenes crecientes aumenta la exposición después de rachas perdedoras.
4. Debido a que las distancias de stop-loss y take-profit se derivan del rango bruto, los valores grandes de `RangePercentage` conducen a compensaciones protectoras igualmente grandes.
