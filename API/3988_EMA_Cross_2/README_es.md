# EMA Estrategia cruzada 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader 4 **"EMA_CROSS_2"** del repositorio MQL. El EA original monitorea dos promedios móviles exponenciales (EMA) y coloca órdenes de mercado cada vez que los promedios intercambian órdenes. La conversión mantiene la naturaleza contraria del script: compra cuando el EMA largo se mueve por encima del EMA corto y vende cuando el EMA corto sube por encima del EMA largo), mientras envuelve la lógica en la infraestructura estratégica de alto nivel StockSharp.

La estrategia opera con velas completadas proporcionadas por el tipo de datos de vela configurable. Las señales se evalúan al cerrar la vela para evitar activaciones repetidas dentro de la misma barra. La gestión de riesgos imita el comportamiento de MetaTrader mediante el uso de distancias de obtención de beneficios, stop loss y trailing stop expresadas en puntos del corredor (escalones de precios).

## Lógica de trading
1. **Cálculo del indicador**
   - Calcule las EMA de período corto y largo en cada vela completa.
   - Omita la primera actualización del indicador, que coincide con el indicador `first_time` original que ignoró la primera evaluación.
   - Luego, detecte un cambio de dirección cuando se invierta el orden relativo entre el EMA largo y corto.
2. **Interpretación de señal**
   - Cuando el EMA largo se mueve por encima del EMA corto, el EA original abrió una operación de compra. El puerto StockSharp mantiene esta regla contraria a pesar de que se comporta de manera opuesta a un sistema cruzado clásico.
   - Cuando el EMA corto cierra por encima del EMA largo, la estrategia abre una operación de venta.
   - Solo se permiten nuevas posiciones cuando no hay ninguna exposición abierta actualmente, lo que replica la condición `OrdersTotal() < 1`.
3. **Ejecución de orden**
   - Las operaciones se envían como órdenes de mercado con un volumen fijo configurable.
   - Al entrar, la estrategia registra los precios de stop-loss y take-profit utilizando la distancia de pip proporcionada a través de los parámetros.
4. **Gestión de riesgos**
   - En cada vela terminada, la estrategia verifica si la acción del precio tocó los niveles almacenados de stop-loss o take-profit. Al superar cualquiera de los niveles, se cierra toda la posición con una orden de mercado.
   - Se aplica un trailing stop (también definido en los puntos del corredor) una vez que el precio se mueve favorablemente más que la distancia de seguimiento. Para posiciones largas el tope protector se desplaza hacia arriba; para posiciones cortas, sigue el precio hacia abajo.
   - Cuando la posición se vuelve plana, se borran los niveles de protección almacenados.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas utilizadas para cálculos de indicadores y detección de señales. | plazo de 15 minutos |
| `OrderVolume` | Volumen de cada orden de mercado en lotes/contratos. | 2 |
| `TakeProfitPoints` | Distancia al nivel de toma de ganancias expresada en puntos de corredor (escalones de precio). Un valor de `0` deshabilita la toma de ganancias. | 20 |
| `StopLossPoints` | Distancia al nivel de stop-loss expresada en puntos del corredor. Un valor de `0` desactiva el stop-loss. | 30 |
| `TrailingStopPoints` | Distancia utilizada al seguir la posición abierta. `0` desactiva el trailing stop. | 50 |
| `ShortEmaPeriod` | Duración de la EMA rápida. | 5 |
| `LongEmaPeriod` | Duración del EMA lenta. | 60 |

## Notas de implementación
- La estrategia utiliza `SubscribeCandles().Bind(shortEma, longEma, ProcessCandle)` para conectar datos de velas con indicadores EMA, siguiendo el patrón preferido de alto nivel API.
- Los valores del indicador se reciben como decimales listos para usar en la devolución de llamada vinculante, por lo que no es necesaria la indexación manual del búfer.
- Las distancias de protección se convierten de MetaTrader puntos a precios de StockSharp multiplicando por el instrumento `PriceStep`. Si el instrumento utiliza un precio de pip fraccionario (3 o 5 decimales), el asistente calcula el tamaño del pip en consecuencia.
- El comportamiento de stop-loss, take-profit y trailing se implementan internamente con las salidas del mercado porque StockSharp no expone el mismo flujo de trabajo `OrderModify` que MetaTrader 4. La gestión comercial resultante refleja la lógica original: los niveles se verifican en cada vela y las salidas ocurren inmediatamente una vez que se superan.
- La primera evaluación cruzada se ignora intencionalmente para reproducir la protección `first_time` que evitó transacciones prematuras en el script MQL.

## Diferencias con la versión MetaTrader
- Gestión del dinero: el EA original siempre intercambiaba el parámetro `Lots`. La conversión expone el mismo concepto a través de `OrderVolume` y también lo asigna a la propiedad de estrategia `Volume` para que los diseñadores y optimizadores puedan reutilizarlo.
- Colocación de órdenes: MetaTrader aplicó stop-loss y take-profit directamente dentro de `OrderSend`. En StockSharp estos niveles son rastreados por la estrategia y cerrados con órdenes de mercado cuando se superan.
- Precisión de trailing stop: las paradas movidas EA utilizan datos de tick (`Bid`/`Ask`). El puerto actualiza la lógica final al cerrar la vela, que es la granularidad más fina disponible dentro de este proyecto de muestra. Las reglas de distancia y activación siguen siendo idénticas.
- Se simplificaron el manejo y el registro de errores; El registro StockSharp proporciona información detallada a través del registro de estrategia estándar.

## Consejos de uso
- Alinee `CandleType` con el período de tiempo utilizado durante las pruebas retrospectivas del EA original para mantener un comportamiento de indicador comparable.
- Cuando opere con símbolos cotizados con pips fraccionarios, asegúrese de que las distancias de puntos configuradas reflejen el número deseado de pips (por ejemplo, en EURUSD `10` puntos equivalen a 1 pip).
- Establezca `OrderVolume` en el tamaño del contrato esperado por su lugar de ejecución. La estrategia no realiza escalado de volumen automático.
- Utilice los controles de optimización integrados en cada parámetro para explorar combinaciones de EMA períodos y distancias de riesgo tal como optimizaría las entradas en MetaTrader.

## Archivos
- `CS/EmaCross2Strategy.cs` – StockSharp implementación de la lógica comercial.
- `README.md` – Documentación en inglés (este archivo).
- `README_zh.md` – Traducción al chino.
- `README_ru.md` – traducción al ruso.
