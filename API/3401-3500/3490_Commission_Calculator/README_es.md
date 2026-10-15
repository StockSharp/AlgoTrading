# Estrategia de calculadora de comisiones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de calculadora de comisiones** es una estrategia de utilidad que refleja el script original MetaTrader. Envía una única orden discrecional utilizando el modo de ejecución seleccionado (mercado, límite o parada) y mide la comisión del corredor aplicada a cada ejecución resultante. La estrategia almacena la comisión acumulada e imprime un informe final con el saldo inicial, las tarifas totales y el saldo de tarifas ajustadas cuando finaliza.

A diferencia de las estrategias convencionales basadas en señales, no se requieren datos ni indicadores de mercado. La estrategia se centra en la contabilidad automatizada de tarifas para ejecuciones manuales o semimanuales.

## Lógica de trading
1. Cuando comienza la estrategia, captura el saldo inicial de la cartera y configura el volumen comercial predeterminado.
2. Los niveles de protección de pérdidas y toma de ganancias opcionales se activan a través de `StartProtection` cuando tanto el precio de entrada como el precio objetivo son válidos. Las distancias se calculan en unidades de precio absoluto, imitando la implementación de MQL.
3. El modo de orden configurado se ejecuta exactamente una vez. Si los parámetros son inconsistentes (por ejemplo, falta el precio de entrada para las órdenes limitadas), la estrategia registra el problema y omite el envío de la orden.
4. Cada operación propia recibida a través de `OnNewMyTrade` se procesa para calcular la comisión utilizando la tasa porcentual configurada.
5. La estrategia agrega todas las comisiones, recuerda la tarifa más reciente y registra un resumen detallado al detenerse.

La implementación supone que la tarifa del corredor es proporcional a `price × volume × commissionRate / 100`. Ajuste la tarifa para que coincida con el lugar que se está modelando.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Quantity` | `0.001` | Volumen comercial enviado por métodos auxiliares (`BuyMarket`, `SellLimit`, etc.). |
| `EntryPrice` | `31365` | Precio utilizado para órdenes límite o stop y para calcular distancias de protección. |
| `StopLossPrice` | `31200` | Precio que define la distancia del stop-loss. Una distancia no positiva desactiva la protección stop-loss. |
| `TakeProfitPrice` | `32100` | Precio que define la distancia de toma de ganancias. Una distancia no positiva desactiva la protección de obtención de beneficios. |
| `CommissionRate` | `0.04` | Tasa de comisión expresada como porcentaje del nocional negociado. |
| `Mode` | `None` | Tipo de orden a ejecutar cuando comience la estrategia. Opciones: `None`, `MarketBuy`, `MarketSell`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`. |

## Notas y mejores prácticas
- Iniciar la estrategia en una cartera que admita la colocación de pedidos manuales; no se requieren suscripciones de datos.
- Asegúrese de que el modelo de comisión del corredor coincida con el parámetro `CommissionRate` para evitar subestimar o sobreestimar las tarifas.
- Para órdenes pendientes, establezca `EntryPrice` en un nivel válido antes de lanzar la estrategia; de lo contrario el pedido no se envía.
- Cuando los niveles de protección están habilitados, la estrategia indica al conector que utilice las salidas del mercado al activarse para imitar fielmente el comportamiento original de MQL.

## Informe de resultados
Cuando se invoca `OnStopped`, la estrategia registra:
- Instantánea del saldo inicial (tomada cuando comenzó la estrategia).
- Tarifas de corretaje agregadas para todas las operaciones procesadas.
- Saldo final ajustado restando las cuotas acumuladas.

Esto hace que la estrategia sea muy adecuada para análisis rápidos de hipótesis y para validar los cronogramas de comisiones de los corredores durante las pruebas retrospectivas.
