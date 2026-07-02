# Estrategia de sorteo aleatorio de la máquina de pinball
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión StockSharp directa del MetaTrader 4 asesores expertos `Pinball_machine.mq4`. El robot original dibujó
enteros aleatorios en cada tick entrante y abría una orden de mercado cada vez que dos de esos sorteos coincidían. La versión StockSharp
conserva el mismo comportamiento estilo lotería: en cada vela terminada del período de tiempo seleccionado, el algoritmo realiza dos pares de
sorteos aleatorios y entra en una posición de mercado larga o corta cuando el par correspondiente contiene valores iguales. Stop-loss y take-profit
las distancias también se aleatorizan en cada evaluación, reproduciendo la sensación de la rutina original de "pinball" donde las operaciones rebotan y
salir de manera impredecible.

## Lógica comercial
- Suscríbase a las velas definidas por el parámetro `CandleType` y espere las barras completamente formadas.
- Por cada vela terminada genere cuatro números enteros distribuidos uniformemente en `[0, RandomMaxValue]`. El primer par pertenece al
entrada potencial en largo, el segundo par pertenece a la entrada potencial en corto.
- Dibuja dos números enteros adicionales entre `MinStopLossPoints`/`MaxStopLossPoints` y `MinTakeProfitPoints`/`MaxTakeProfitPoints` para
determinar las distancias de protección (expresadas en incrementos de precios) compartidas por ambos lados de la evaluación.
- Si el primer y segundo entero aleatorio coinciden, envíe una orden de compra de mercado con el volumen `TradeVolume`. Si el tercero y el cuarto
los valores coinciden, envíe una orden de venta de mercado con el mismo volumen. Ambas condiciones pueden dispararse dentro de la misma vela, exactamente como en
la versión MQL donde las órdenes de compra y venta eran eventos independientes.
- Adjunte inmediatamente una orden de stop-loss y take-profit (si la distancia trazada es mayor que cero). Las distancias se interpretan.
como múltiplos del `PriceStep` del instrumento, reflejando el multiplicador `Point` utilizado en MetaTrader.

## Gestión de pedidos y controles de riesgos.
- `StartProtection()` se invoca cuando comienza la estrategia para que StockSharp gestione las órdenes de protección en nombre de la estrategia.
- Cada entrada mide la posición resultante (`Position ± TradeVolume`) y la pasa a `SetStopLoss` y `SetTakeProfit`, que
permite a la plataforma consolidar órdenes de protección incluso cuando se ejecutan varias operaciones al mismo tiempo.
- Si los parámetros de distancia mínima o máxima se establecen en cero o en un número negativo, se activa la protección correspondiente.
omitido para ese ciclo.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño del pedido en lotes/contratos enviados para cada entrada aleatoria. |
| `CandleType` | Periodo de tiempo de las velas que desencadenan los sorteos aleatorios. Los períodos más cortos emulan más fielmente el EA original basado en ticks. |
| `RandomMaxValue` | Límite superior inclusivo para los sorteos de números enteros. Un valor mayor reduce la probabilidad de que coincidan los números y, por lo tanto, reduce la frecuencia comercial. |
| `MinStopLossPoints` | Límite inferior (en pasos de precio) para la distancia de stop-loss generada aleatoriamente. |
| `MaxStopLossPoints` | Límite superior (en pasos de precio) para la distancia de stop-loss. |
| `MinTakeProfitPoints` | Límite inferior (en incrementos de precios) para la distancia de obtención de beneficios generada aleatoriamente. |
| `MaxTakeProfitPoints` | Límite superior (en incrementos de precio) para la distancia de obtención de beneficios. |
| `RandomSeed` | Semilla del generador de números pseudoaleatorios. Cero mantiene el comportamiento basado en el tiempo, cualquier otro valor hace que la secuencia sea reproducible. |

## Notas de implementación
- El script MetaTrader estaba controlado por ticks; el puerto StockSharp utiliza finalizaciones de velas porque el API de alto nivel opera en eventos de series de tiempo. Establecer un `CandleType` muy corto (por ejemplo, velas de un segundo o tick) restaura la naturaleza rápida del original.
- Los valores de stop-loss y take-profit se generan una vez por evaluación y se reutilizan tanto para las ramas largas como para las cortas, exactamente como en la fuente EA.
- Asegúrese de que el instrumento negociado exponga un `PriceStep` válido; de lo contrario, las distancias de protección expresadas en puntos pueden necesitar un ajuste manual.
