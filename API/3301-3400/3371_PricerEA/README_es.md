# Estrategia PricerEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia PricerEA** recrea el comportamiento del MetaTrader 4 experto "PricerEA v1.0" usando la API de alto nivel de StockSharp.
Coloca hasta cuatro órdenes pendientes (stop de compra, stop de venta, límite de compra y límite de venta) a niveles de precios definidos manualmente. Una vez cualquiera
orden pendiente se completa, la estrategia adjunta órdenes protectoras de stop-loss y take-profit, habilitando opcionalmente un trailing stop y
ajuste de equilibrio para seguir el Asesor Experto original.

## como funciona

1. **Órdenes pendientes**: la estrategia lee los niveles de precios absolutos de las entradas y envía únicamente las órdenes pendientes correspondientes.
una vez al inicio. La caducidad opcional se puede configurar en minutos.
2. **Selección de volumen**: los usuarios pueden mantener el tamaño de lote manual fijo o cambiar al modo automático desde donde se deriva el volumen.
el saldo de la cartera y el análogo del factor de riesgo MT4.
3. **Protección**: después de completar una orden de entrada, la estrategia crea órdenes de limitación de pérdidas y toma de ganancias a la distancia configurada.
(expresado en puntos de precio). Cuando se habilitan tanto el seguimiento como el punto de equilibrio, la parada sigue las condiciones originales MQL: es
se mueve solo después de que el precio cubre la distancia de equilibrio más la parada inicial.
4. **Mantenimiento de pedidos**: los pedidos pendientes se cancelan automáticamente cuando expira su vida útil o cuando se detiene la estrategia.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `BuyStopPrice`, `SellStopPrice`, `BuyLimitPrice`, `SellLimitPrice` | Precios absolutos de las correspondientes órdenes pendientes. Un valor de `0` deshabilita el pedido. |
| `TakeProfitPoints` | Distancia desde el precio de entrada hasta la orden de toma de ganancias, medida en puntos de precio (`Security.PriceStep`). |
| `StopLossPoints` | Distancia desde el precio de entrada hasta la orden de stop-loss, también medida en puntos de precio. |
| `EnableTrailingStop` | Habilita la lógica del trailing stop. |
| `TrailingStepPoints` | Se requiere un movimiento mínimo (en puntos) antes de mover el tope móvil. |
| `EnableBreakEven` | Habilita la regla de equilibrio que eleva el stop por encima/por debajo de la entrada después de obtener ganancias suficientes. |
| `BreakEvenTriggerPoints` | Se requiere ganancia adicional (puntos) antes de que se mueva el tope para alcanzar el punto de equilibrio. |
| `PendingExpiryMinutes` | Vida útil de las órdenes pendientes en minutos. `0` los mantiene vivos hasta que se llenen o se cancelen manualmente. |
| `VolumeMode` | Elige entre volumen manual y dimensionamiento automático. |
| `RiskFactor` | Multiplicador de riesgo utilizado por el dimensionamiento automático (refleja la entrada MQL). |
| `ManualVolume` | Tamaño de lote fijo utilizado cuando `VolumeMode` se establece en `Manual`. |

## Diferencias vs. la versión MT4

- El cálculo automático del volumen utiliza el saldo de la cartera StockSharp y el multiplicador del contrato de valores. Diferentes corredores
Puede utilizar fórmulas distintas, por lo que el valor resultante puede diferir ligeramente de MetaTrader.
- Las órdenes de protección se realizan a través de StockSharp ayudantes y respetan el paso de volumen del lugar, el volumen mínimo y máximo.
- La caducidad se implementa dentro de la estrategia (MetaTrader depende de la caducidad de la orden del lado del servidor).

## Notas de uso

- Configura los niveles de precios antes de iniciar la estrategia. Valores iguales a cero dejan inhabilitado el orden correspondiente.
- Para imitar la lógica de "Dígitos" de MT4, los parámetros basados en puntos operan en `Security.PriceStep` unidades.
- Combine la estrategia con la cartera y las herramientas de registro de StockSharp para monitorear las órdenes pendientes y las paradas de protección.
