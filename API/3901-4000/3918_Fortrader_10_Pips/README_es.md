# Estrategia Fortrader 10 Pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Fortrader 10 Pips** es una StockSharp versión del MetaTrader 4 asesor experto `10pips.mq4` (ID de estrategia 8074). El robot mantiene abiertas simultáneamente una posición larga y una corta. Cada tramo utiliza distancias fijas de toma de ganancias, stop-loss y trailing-stop medidas en puntos de símbolo.

Esta conversión recrea el comportamiento de cobertura dentro del nivel alto de StockSharp API. Inmediatamente después de que comienza la estrategia, envía una orden de compra y venta de mercado. Siempre que una orden de protección cierra un tramo, la estrategia abre instantáneamente una nueva orden en la misma dirección, manteniendo vivas dos posiciones opuestas en todo momento.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Take Profit Buy` | Distancia de toma de ganancias para el tramo largo, en puntos. |
| `Stop Loss Buy` | Distancia de stop-loss para el tramo largo, en puntos. |
| `Trailing Stop Buy` | Distancia del trailing-stop para el tramo largo, en puntos. Establezca en cero para desactivar el seguimiento. |
| `Take Profit Sell` | Distancia de toma de ganancias para el tramo corto, en puntos. |
| `Stop Loss Sell` | Distancia de stop-loss para el tramo corto, en puntos. |
| `Trailing Stop Sell` | Distancia del trailing-stop para el tramo corto, en puntos. Establezca en cero para desactivar el seguimiento. |
| `Volume` | Volumen de cada orden de mercado en lotes. |

Todas las distancias se multiplican por el `PriceStep` del instrumento para convertir de puntos a valores de precio absoluto. Cada parámetro se expone a través de `StrategyParam<T>` para que la estrategia se pueda ajustar u optimizar a través de la GUI.

## Lógica de trading
1. **Inicio**: `OnStarted` se suscribe a los datos de nivel 1 para realizar un seguimiento de los mejores precios de oferta y demanda actuales. La estrategia envía inmediatamente una orden de compra de mercado y una orden de venta de mercado.
2. **Órdenes de protección**: después de cada entrada completa (`OnNewMyTrade`), la estrategia crea las órdenes de stop-loss y take-profit asociadas si las distancias son mayores que cero. Las órdenes se redondean al paso de precio más cercano.
3. **Reentrada**: cuando se ejecuta una orden de limitación de pérdidas o de toma de ganancias, el tramo cerrado se reabre instantáneamente con una nueva orden de mercado para que persista la exposición bidireccional.
4. **Paradas dinámicas**: las actualizaciones de nivel 1 activan `UpdateTrailingStops`, que ajusta las órdenes de limitación de pérdidas cada vez que la oferta/demanda actual se ha movido más allá de la distancia final configurada desde el precio de entrada. La lógica refleja la EA original: el seguimiento comienza una vez que las ganancias exceden la distancia de seguimiento, y las paradas se mueven solo en la dirección de las ganancias.

## Notas de implementación
- El código MT4 original esperó 10 segundos entre las órdenes iniciales de compra y venta. StockSharp no requiere este retraso, por lo tanto ambos pedidos se envían de inmediato.
- Debido a que StockSharp utiliza posiciones netas de forma predeterminada, la verdadera cobertura puede depender de que el corredor/conector respalde posiciones opuestas. La estrategia realiza un seguimiento de cada tramo de forma independiente y los restablece después de cada salida.
- `StartProtection()` se llama una vez durante `OnStarted` para que las protecciones de riesgos globales estén activas si se configuran en la configuración del marco.

## Consejos de uso
- Asegúrese de que el conector seleccionado admita posiciones largas y cortas simultáneas si se requiere el comportamiento de cobertura.
- Establezca las distancias de seguimiento en cero para desactivar el seguimiento del tramo correspondiente.
- Optimice los parámetros de riesgo (`Take Profit`, `Stop Loss`, `Trailing Stop`) en datos históricos para que se ajusten al símbolo negociado y al período de tiempo.
