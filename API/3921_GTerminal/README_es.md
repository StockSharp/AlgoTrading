# Estrategia GTerminal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia GTerminal es un puerto C# del asesor experto MetaTrader 4 `GTerminal_V5a`. El guión original permitía el manual.
Control de entradas y salidas dibujando líneas horizontales en el gráfico. Este puerto recrea el mismo comportamiento basado en líneas dentro
el marco StockSharp exponiendo cada línea virtual como un parámetro configurable. Siempre que el precio de cierre del seleccionado
La serie de velas cruza una de estas líneas virtuales, la estrategia abre, cierra o invierte posiciones de la misma manera que el MQL4
versión. Los niveles de protección automática opcionales emulan las líneas auxiliares "tpinit" y "slinit" de la herramienta original.

## Lógica estratégica
### Muestreo de precios
* La estrategia funciona en velas terminadas de un período de tiempo definido por el usuario (`CandleType`).
* `StartShift` controla qué vela se utiliza como cierre de referencia. Un valor de `0` usa el cierre de vela actual, `1` usa el
vela anterior, etc. El cambio también afecta a la vela de comparación, por lo que el script siempre evalúa dos cierres consecutivos como el
MetaTrader implementación.
* `CrossMethod` refleja la entrada MQL4:
  * `0` – cruce estricto: el cierre anterior debe estar por debajo (para disparadores largos) o por encima (para disparadores cortos) del nivel y el
El cierre actual debe terminar en el lado opuesto del nivel.
  * `1` – disparador instantáneo: el cierre actual solo necesita estar por encima/por debajo del nivel. El puerto aún verifica el cierre anterior de
evitar múltiples activadores en la misma barra, replicando el comportamiento de "tocar una vez" obtenido en MetaTrader al eliminar la línea
después de que se dispara.

### Reglas de entrada
* **Línea de parada de compra**: cuando el cierre se mueve desde abajo hacia arriba `BuyStopLevel`, la estrategia compra. Si hay una posición corta abierta,
el tamaño del pedido incluye el volumen necesario para aplanar la exposición corta más el `Volume` configurado para la nueva exposición larga.
* **Línea de límite de compra**: cuando el cierre cae hasta `BuyLimitLevel`, se abre una posición larga usando la misma lógica de volumen.
* **Línea de parada de venta**: cuando el cierre se mueve de arriba a abajo `SellStopLevel`, la estrategia vende. Los largos existentes se cierran como
parte de la cantidad del pedido.
* **Línea de límite de venta**: cuando el cierre sube hasta `SellLimitLevel`, se abre una posición corta.
* Las entradas se ignoran cuando `Volume` es `0` o `PauseTrading` está habilitado.

### reglas de salida
* **Salidas direccionales** – `LongStopLevel` y `LongTakeProfitLevel` cierran el lado largo cuando el cierre cruza el respectivo
línea. `ShortStopLevel` y `ShortTakeProfitLevel` hacen lo mismo para exposiciones breves.
* **Salidas globales**: `AllLongStopLevel` / `AllLongTakeProfitLevel` liquida cada posición larga independientemente de cómo se abrió.
`AllShortStopLevel` / `AllShortTakeProfitLevel` reflejan la lógica de los cortos.
* **Protección inicial**: configurar `UseInitialProtection` en `true` aplica `InitialLongStopLevel`, `InitialLongTakeProfitLevel`,
`InitialShortStopLevel` y `InitialShortTakeProfitLevel` inmediatamente después de que se cubra un nuevo puesto. Estos niveles se comportan como el
Líneas auxiliares "slinit" / "tpinit" del script original y permanecen activas hasta que se cierra la posición o se actualiza el nivel.
* Solo se envía una acción de salida por vela. Cuando se cumple una condición de salida, la estrategia envía la orden de cierre y se salta la
comprobaciones restantes para esa barra, justo cuando la versión MQL4 se detuvo después de que se disparó la línea.

### control de pausa
* `PauseTrading` reproduce la funcionalidad de la línea MetaTrader "PAUSA". Cuando está habilitado, no se evalúa ninguna lógica de entrada o salida.
El estado se puede cambiar manualmente sin recargar la estrategia.

## Parámetros
* **Volumen** – volumen de pedidos para nuevas entradas. El tamaño final del pedido incluye automáticamente cualquier exposición opuesta que deba ser
cerrado durante una reversión.
* **Método cruzado**: seleccione el algoritmo de cruce (`0` estricto, `1` instantáneo).
* **Start Shift**: compensación de vela utilizada para el cálculo del cruce.
* **Pausar operaciones**: desactiva todas las acciones comerciales mientras `true`.
* **Usar protección inicial**: permite la aplicación automática de los niveles iniciales de parada/toma de ganancias después de cada llenado.
* **Nivel de parada de compra/Nivel de límite de compra**: niveles de precios que activan entradas largas.
* **Nivel de parada de venta/Nivel de límite de venta**: niveles de precios que activan entradas cortas.
* **Nivel de parada larga/Take Profit larga**: líneas de salida para la posición larga activa.
* **Nivel de parada corta/Take Profit en corto**: líneas de salida para la posición corta activa.
* **All Long Stop / All Long Take Profit**: líneas de salida globales que cierran cada posición larga.
* **All Short Stop / All Short Take Profit**: líneas de salida globales que cierran cada posición corta.
* **Initial Long Stop / Inicial Long Take Profit** – niveles de protección activados después de cada entrada larga cuando la protección inicial es
habilitado.
* **Parada corta inicial/Take Profit inicial en corta**: niveles de protección activados después de cada entrada corta cuando la protección inicial está activa.
habilitado.
* **Tipo de vela**: período de tiempo que proporciona los precios de cierre utilizados para las comparaciones.

## Notas de implementación
* El puerto mantiene el flujo de trabajo basado en líneas pero expone cada línea como un parámetro en lugar de depender de los objetos del gráfico. Los usuarios pueden
actualice los niveles sobre la marcha a través de la cuadrícula de parámetros, imitando la forma en que se movieron las líneas en un gráfico MetaTrader.
* Los activadores de ventana de indicador del script original (RSI, CCI, Momentum, etc.) no están disponibles en esta versión. Todos los desencadenantes
Utilice únicamente precios de cierre. El conjunto de parámetros aún se puede combinar con otros componentes StockSharp si el comportamiento está basado en indicadores.
es necesario.
* La estrategia se basa únicamente en órdenes de mercado (`BuyMarket`, `SellMarket`) al igual que el script MQL4, que utilizaba órdenes de mercado para
emular la ejecución de la línea pendiente.
* No existe una implementación de Python; En este paquete solo se proporciona la versión C#.
