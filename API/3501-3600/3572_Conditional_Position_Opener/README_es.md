# Estrategia de apertura de posición condicional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de apertura de posición condicional** reproduce el comportamiento del script MetaTrader original *"Abrir una posición de compra si no hay una posición abierta"*. La lógica es intencionalmente simple: cuando los cambios manuales habilitan el lado largo o corto, la estrategia envía una orden de mercado sólo si no hay exposición abierta en esa dirección. Esto evita entradas duplicadas y mantiene la posición alineada con el lado seleccionado.

El puerto StockSharp mantiene la implementación neutral para los intermediarios al confiar en la suscripción de vela de alto nivel del marco y el asistente de protección integrado. Las distancias de stop-loss y take-profit se proporcionan en unidades de pips (escalones de precio) para que puedan adaptarse a cualquier instrumento.

## Lógica de la estrategia
1. Suscríbase a la serie de velas configuradas para actuar como un latido del corazón.
2. En cada vela terminada verifique la posición neta actual.
3. Si el interruptor largo está habilitado y la posición es plana o corta, envíe una orden de compra de mercado.
4. Si el interruptor corto está habilitado y la posición es plana o larga, envíe una orden de venta en el mercado.
5. Las órdenes de protección se gestionan automáticamente a través de `StartProtection`, que convierte las distancias de pips en compensaciones de precios reales.

Debido a que StockSharp usa posiciones netas, habilitar ambas partes al mismo tiempo abrirá primero la operación larga y luego, si sigue sin cambios después de completarse, la operación corta. Esto refleja la intención del código MQL que evitaba múltiples pedidos por dirección.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `1` | Tamaño de la orden para cada entrada al mercado. |
| `StopLossPips` | `100` | Distancia de stop-loss expresada en pasos de precio. Establezca en cero para desactivar. |
| `TakeProfitPips` | `200` | Distancia de obtención de beneficios expresada en incrementos de precio. Establezca en cero para desactivar. |
| `EnableBuy` | `false` | Cuando `true` la estrategia puede abrir posiciones largas si no existe una exposición larga. |
| `EnableSell` | `false` | Cuando `true` la estrategia puede abrir posiciones cortas si no existe exposición corta. |
| `CandleType` | `1 minute timeframe` | Serie de velas que impulsa la evaluación periódica. |

## Notas
- Las distancias se convierten en incrementos de precios reales utilizando el `PriceStep` del instrumento. Si el intercambio no lo informa, el valor del pip bruto se utiliza como compensación absoluta.
- `StartProtection` adjunta automáticamente órdenes de stop-loss y take-profit después de cada ejecución, por lo que no es necesaria la gestión manual de órdenes.
- La estrategia se centra en la activación de estilo manual y pretende ser una plantilla para la ejecución discrecional a través de parámetros.
