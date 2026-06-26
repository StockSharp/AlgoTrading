# Estrategia de 2DLimits
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
2DLimits es un puerto directo del experto asesor MetaTrader 4 `2DLimits_EA_v2`. La estrategia evalúa las dos últimas velas diarias completadas y solo participa cuando forman un patrón escalonado (máximos y mínimos más altos, o máximos y mínimos más bajos). Cuando el patrón es válido, la estrategia envía órdenes de stop en el extremo del día anterior y protege la posición con un stop-loss en el punto medio y un objetivo igual al rango diario anterior.

La implementación se basa en las suscripciones de velas de alto nivel de StockSharp junto con cotizaciones de nivel 1. Las velas diarias proporcionan los niveles de ruptura mientras que las instantáneas del mejor bid/ask aseguran que las configuraciones largas solo se activen cuando el precio opera por debajo del punto medio y las cortas solo cuando opera por encima.

## Lógica de la estrategia
### Filtro de estructura diaria
* La estrategia mantiene una ventana deslizante de dos días de velas diarias completadas (configurable a través del parámetro de tipo de vela).
* Una **configuración alcista** requiere que el día más reciente registre tanto un máximo más alto como un mínimo más alto en comparación con el día anterior.
* Una **configuración bajista** requiere que el día más reciente presente tanto un máximo más bajo como un mínimo más bajo que el día anterior.
* El punto medio del día más reciente se calcula como `(high + low) / 2`, y el rango de la vela se almacena para el objetivo de beneficio.

### Reglas de entrada
* Solo hay un lote de órdenes pendientes activas a la vez; todas las órdenes se cancelan y recalculan cuando se cierra una nueva vela diaria.
* Las entradas largas se preparan cuando:
  * El filtro de estructura alcista está satisfecho.
  * El último precio ask está por debajo del punto medio del día anterior (refleja el control `Ask < middleY` del EA original).
  * Se coloca una orden de compra-stop exactamente en el máximo del día anterior.
* Las entradas cortas se preparan cuando:
  * El filtro de estructura bajista está satisfecho.
  * El último precio bid está por encima del punto medio del día anterior (refleja `Bid > middleY`).
  * Se coloca una orden de venta-stop en el mínimo del día anterior.
* Si ambas comprobaciones de estructura fallan, no se dejan órdenes activas para la próxima sesión.

### Reglas de salida
* Cuando una orden de stop se activa, la orden de entrada opuesta se cancela de inmediato para que la estrategia nunca mantenga exposiciones largas y cortas simultáneas.
* Tras una ruptura larga, se registran dos órdenes protectoras:
  * Una orden de stop en el punto medio del día de referencia actúa como stop-loss.
  * Una orden de take-profit en `máximo anterior + rango anterior` coincide con la distancia de take-profit de MetaTrader.
* Tras una ruptura corta, se aplica protección simétrica:
  * Una orden de stop en el punto medio (buy-stop) cubre el stop-loss.
  * Una orden de take-profit en `mínimo anterior - rango anterior` refleja el objetivo original.
* Las órdenes protectoras se reactivan cada vez que cambia el tamaño de la posición ejecutada y se eliminan una vez que la posición vuelve a ser plana.

### Ciclo de vida de la orden y comprobaciones de seguridad
* Las órdenes pendientes se actualizan solo después de que se completa la siguiente vela diaria, imponiendo una única configuración por día de trading.
* La estrategia omite la generación de señales siempre que ya tenga una posición, evitando superposiciones entre configuraciones.
* La última instantánea de bid/ask se retiene de `SubscribeLevel1()`; si no está disponible, el último precio de transacción se usa como respaldo para evitar enviar órdenes ciegas.
* El redondeo se realiza con el paso de precio del instrumento para que todas las órdenes se alineen con el tamaño de tick del mercado.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Volume` | Volumen de la orden para las entradas de stop. Debe ser mayor que cero. |
| `CandleType` | Tipo de vela que proporciona el rango de referencia (predeterminado velas diarias). |

## Notas adicionales
* La estrategia gestiona cada orden directamente a través de la API de alto nivel; no hay dependencia de colecciones personalizadas o buffers de indicadores.
* Solo se proporciona la implementación C# en este paquete. No se crea versión Python para esta conversión.
* Las pruebas no se modifican según se solicitó.
