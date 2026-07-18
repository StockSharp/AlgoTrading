# Estrategia LazyBot V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

LazyBot V1 es una estrategia de ruptura diaria convertida del asesor experto original MetaTrader 5. Cada día de negociación, coloca un par de órdenes stop pendientes alrededor del rango de precios del día anterior y utiliza un stop dinámico para proteger las posiciones abiertas. La conversión aprovecha el StockSharp API de alto nivel con suscripciones de velas y gestión automática de pedidos.

## Lógica de trading

1. Espere a que se complete una vela del período de tiempo configurado (diariamente de forma predeterminada).
2. En un nuevo día, opcionalmente, asegúrese de que la hora actual del servidor esté dentro de la ventana comercial permitida y omita los fines de semana.
3. Cancele cualquier orden pendiente de ruptura existente creada por la estrategia.
4. Coloque un stop de compra por encima del máximo del día anterior y un stop de venta por debajo del mínimo del día anterior. El parámetro `Breakout Offset (pips)` agrega distancia adicional a ambos niveles de ruptura.
5. Cuando se active cualquiera de las órdenes, mantenga el stop-loss protector a una distancia fija y sígalo cada vez que el precio avance a favor de la operación por más de la distancia de pips configurada.
6. Vuelva a calcular el volumen para los próximos pedidos utilizando un tamaño de lote fijo o el módulo de dimensionamiento basado en riesgos.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| Tipo de vela | Periodo de tiempo utilizado para recopilar las velas de referencia (diario por defecto). |
| Nombre del robot | Valor escrito en los comentarios del pedido para facilitar el seguimiento. |
| Stop Loss (pips) | Distancia utilizada tanto para la parada inicial como para la parada final. |
| Compensación de ruptura (pips) | Distancia adicional aplicada al máximo/mínimo anterior al realizar las órdenes pendientes. |
| Spread máximo (pips) | Spread máximo permitido antes de crear nuevas órdenes de ruptura. Establezca en 0 para desactivar la verificación. |
| Utilice el horario comercial | Habilita el filtro de hora de inicio similar al EA original. |
| Hora de inicio | Primera hora (inclusive) en la que se podrán realizar nuevos pedidos. |
| Hora final | Hora a la que se deja de programar nuevos pedidos. Cuando es igual a la hora de inicio, el filtro actúa como un límite inferior simple. |
| % de riesgo de uso | Permite el cálculo de volumen basado en riesgos. |
| % de riesgo | Porcentaje del capital de la cartera utilizado para dimensionar las posiciones cuando `Use Risk %` está habilitado. |
| Volumen fijo | Volumen de orden fijo utilizado cuando el tamaño del riesgo está deshabilitado. Cuando es cero, la estrategia vuelve a la propiedad global `Volume` (el valor predeterminado es 0,01). |

## Gestión del riesgo

* El stop dinámico refleja la lógica de seguimiento MetaTrader al mantener el stop loss `Stop Loss (pips)` alejado de la mejor oferta/demanda y solo se ajusta cuando se alcanza un mejor precio.
* El filtro de diferencial protege la estrategia de enviar nuevas órdenes de ruptura cuando el mercado es demasiado amplio.
* El dimensionamiento basado en el riesgo divide el riesgo monetario permitido (`equity * Risk %`) por la distancia de parada expresada en unidades de precio y nunca desciende por debajo del tamaño de lote fijo.

## Notas adicionales

* Los comentarios de pedidos siguen el formato `BotName;SymbolId;YYYYMMDD`, lo que facilita distinguir los pedidos pendientes creados en diferentes días.
* La estrategia se suscribe a los datos de Level1 para evaluar el diferencial actual del filtro y realizar paradas de seguimiento con los últimos valores de oferta/demanda.
* Las paradas dinámicas se vuelven a aplicar en cada actualización de vela e inmediatamente después se llenan para coincidir con el comportamiento original de EA.
