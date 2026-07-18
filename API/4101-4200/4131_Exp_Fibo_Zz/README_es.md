# Estrategia EXP FIBO ZZ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia EXP FIBO ZZ es un puerto C# del asesor experto MetaTrader 4 `EXP_FIBO_ZZ_V1en`. Reproduce la ruptura original.
Lógica: monitorear el último corredor de ZigZag confirmado, colocar un stop de compra por encima del máximo y un stop de venta por debajo del mínimo, y
adjunte órdenes de stop-loss y take-profit basadas en Fibonacci. La versión StockSharp expone todas las entradas configurables a través de
`StrategyParam` objetos, agrega una validación extensa y mantiene las opciones originales de administración de dinero, incluido el riesgo basado en el saldo
dimensionamiento y ajuste del punto de equilibrio.

## Lógica de trading
1. **Preparación de datos**
   - La estrategia se suscribe al `CandleType` configurado (predeterminado: velas de 1 minuto) y alimenta la serie en `Highest` y
`Lowest` indicadores con una longitud igual a `ZigZagDepth`.
   - Un detector ZigZag liviano rastrea los tres precios de pivote más recientes. Un nuevo pivote se registra sólo cuando:
     * La vela alta/baja es igual a la salida del indicador.
     * Han pasado al menos `ZigZagBackstep` barras desde el punto de inflexión anterior.
     * La desviación del precio del pivote actual supera los `ZigZagDeviationPips` (expresado en MetaTrader pips).

2. **Validación del corredor**
   - Una vez que hay tres pivotes disponibles, los dos más antiguos definen el corredor. El comercio continúa sólo si la altura del corredor está entre
`MinCorridorPips` y `MaxCorridorPips` y el último pivote se encuentra estrictamente dentro de la banda con un pequeño buffer estilo corredor.
   - Fuera de la ventana comercial especificada por el usuario (`StartHour/StartMinute` a `StopHour/StopMinute`), todas las órdenes pendientes se cancelan.

3. **Realización de pedidos**
   - Los precios de parada de compra y venta se calculan como los límites del corredor más/menos `EntryOffsetPips`.
   - La distancia de stop-loss es igual a `corridor * FiboStopLoss / 100`. La distancia de obtención de beneficios sigue la fórmula MetaTrader
`corridor * (FiboTakeProfit / 100 - 1)` con valores negativos fijados en cero.
   - Antes de realizar pedidos, la estrategia calcula el volumen comercial. Si `RiskPercent > 0`, el código multiplica el capital seleccionado
fuente (capital cuando `UseBalanceForRisk` es `true`; de lo contrario, capital menos margen bloqueado) por el porcentaje de riesgo y se divide
el resultado por el precio de referencia. El volumen se ajusta a la cuadrícula del lote de intercambio y se recorta a los límites del intercambio. cuando
la información requerida no está disponible, el algoritmo recurre a `FixedVolume`.
   - Las órdenes de entrada activas se modifican cada vez que cambia el precio objetivo o el volumen; de lo contrario, se envían nuevos pedidos.

4. **Gestión de posiciones**
   - Tan pronto como se abre una posición, el algoritmo cancela la orden pendiente opuesta y registra órdenes de protección:
     * Stop-loss a través de `SellStop`/`BuyStop` en la distancia precalculada.
     * Toma de ganancias opcional a través de `SellLimit`/`BuyLimit`.
   - El módulo de equilibrio opcional (`EnableBreakEven`) refleja la rutina original `MovingInWL`. Después de acumular
`BreakEvenTriggerPips` de beneficio el stop se traslada al precio de entrada más/menos `BreakEvenOffsetPips`, garantizando al menos
una pequeña ganancia y al mismo tiempo evita ajustes repetidos.

5. **Mantenimiento de sesión**
   - Salir de la ventana de negociación o aplanar la posición cancela cualquier orden pendiente o de protección pendiente. el metodo
`OnStopped` también borra todos los pedidos cuando finaliza la estrategia.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `CandleType` | Serie de datos utilizados para construir los pivotes ZigZag. | `1m TimeFrame()` | Admite cualquier tipo de vela StockSharp. |
| `ZigZagDepth` | Número mínimo de velas entre oscilaciones de ZigZag. | `12` | Coincide con la entrada MT4 `ExtDepth`. |
| `ZigZagDeviationPips` | Desviación mínima (en MetaTrader pips) antes de aceptar un nuevo pivote. | `5` | Espejos `ExtDeviation`. |
| `ZigZagBackstep` | Recuento mínimo de barras antes de que el ZigZag pueda retroceder nuevamente. | `3` | Equivalente a `ExtBackstep`. |
| `EntryOffsetPips` | Distancia en pips agregada por encima/debajo del corredor al realizar órdenes stop. | `5` | Espejos `n_pips`. |
| `MinCorridorPips` | Límite inferior para el tamaño del corredor. | `20` | Espejos `Min_Corridor`. |
| `MaxCorridorPips` | Límite superior para el tamaño del corredor. | `100` | Espejos `Max_Corridor`. |
| `FiboStopLoss` | Fibonacci ratio aplicado al corredor para derivar la distancia de stop-loss. | `61.8` | Espejos `Fibo_StopLoss`. |
| `FiboTakeProfit` | Fibonacci ratio aplicado para calcular el objetivo de obtención de beneficios. | `161.8` | Espejos `Fibo_TakeProfit`. |
| `StartHour` / `StartMinute` | Inicio de la sesión de negociación permitida. | `00:01` | Los pedidos se cancelan fuera de la ventana. |
| `StopHour` / `StopMinute` | Fin de la sesión de negociación. | `23:59` | Admite sesiones nocturnas que finalizan la medianoche. |
| `UseBalanceForRisk` | Elija capital (`true`) o efectivo disponible (`false`) para dimensionar el riesgo. | `true` | Espejos `Choice_method`. |
| `RiskPercent` | Fracción del capital destinada a la siguiente operación. | `1` | Establezca en `0` para deshabilitar el tamaño basado en riesgos. |
| `FixedVolume` | Tamaño de lote utilizado cuando el tamaño de riesgo está deshabilitado o no está disponible. | `0.1` | Refleja la entrada `Lots`. |
| `EnableBreakEven` | Habilita el ajuste del tope de equilibrio. | `true` | Espejos `MovingInWL`. |
| `BreakEvenTriggerPips` | Ganancia en pips requerida antes de mover el stop. | `13` | Espejos `LevelProfit`. |
| `BreakEvenOffsetPips` | Compensación en pips aplicada al punto de equilibrio. | `2` | Espejos `LevelWLoss`. |
| `DrawCorridorLevels` | Trace el corredor activo en el gráfico. | `false` | Refleja la bandera de dibujo lineal opcional. |

## Notas de implementación
- La conversión de pips respeta las convenciones de MetaTrader al multiplicar `PriceStep` por 10 para símbolos Forex de tres y cinco dígitos.
- Los precios y volúmenes de los pedidos se redondean al incremento válido más cercano utilizando los metadatos del intercambio (`PriceStep`, `VolumeStep`,
`MinVolume`, `MaxVolume`).
- El tamaño del riesgo retrocede con elegancia cuando faltan datos de la cartera o precios de referencia, lo que garantiza que la estrategia siga funcionando con
el lote fijo configurado.
- La rutina de equilibrio cancela y vuelve a registrar el stop de protección sólo una vez por operación y nunca coloca el stop más allá del
precio de entrada.
- Cuando `DrawCorridorLevels` está habilitado, la estrategia dibuja un segmento vertical entre los pivotes alto y bajo del actual
corredor, lo que permite una rápida confirmación visual del rango de negociación.

## Diferencias frente a la versión MetaTrader
- Se omitieron los objetos gráficos, los sonidos y los comentarios en pantalla del script MT4; StockSharp las primitivas de registro y gráfico cubren
mismas necesidades.
- El dimensionamiento del riesgo utiliza el capital de la cartera y los últimos precios conocidos en lugar de los valores de margen de `MarketInfo`, porque esos detalles son de corredor.
específico y no disponible de manera independiente de la plataforma.
- La gestión de pedidos utiliza el StockSharp API (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) de alto nivel en lugar del ticket manual.
manipulación. El comportamiento sigue siendo equivalente y requiere menos código repetitivo.
- El detector ZigZag vuelve a implementar la lógica de profundidad/desviación/retroceso con indicadores incorporados para seguir siendo compatible con
Modelo de vela en streaming de StockSharp.
