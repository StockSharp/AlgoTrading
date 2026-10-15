# Estrategia envolvente simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia envolvente simple** replica el comportamiento de los MetaTrader 4 expertos "compra simple engulf mt4" y "venta simple engulf mt4". Ambos expertos detectan patrones de velas envolventes y abren operaciones en una sola dirección. El puerto StockSharp fusiona ambos asesores en una estrategia configurable para que el comerciante pueda reproducir el comportamiento original de solo compra, solo venta o combinado dentro del marco StockSharp.

La estrategia solo escucha velas completadas, lo que coincide con el estilo de ejecución de cierre de barra utilizado por la versión MetaTrader. Toda colocación de pedidos utiliza StockSharp API (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket` y `StartProtection`) de alto nivel para mantenerse cerca de las pautas de codificación StockSharp.

## Lógica de trading
1. Construya velas según el `CandleType` configurado.
2. Espere a que termine la vela actual y mantenga en la memoria la vela anterior completada.
3. Calcule el tamaño actual del cuerpo de la vela en pips. Rechazar el patrón cuando esté por debajo de `MinBodyPips` o por encima de `MaxBodyPips` (si el filtro máximo está habilitado con un valor positivo).
4. Detecta un patrón **envolvente alcista** cuando:
   - La vela anterior es bajista (cierra debajo de abierta).
   - La vela actual es alcista (cierre por encima de apertura).
   - La apertura actual es inferior o igual al cierre anterior.
   - El cierre actual es superior o igual a la apertura anterior.
5. Detecte un patrón **envolvente bajista** utilizando las condiciones reflejadas.
6. Cuando aparezca un patrón válido, asegúrese de que se permita el comercio automatizado (`IsFormedAndOnlineAndAllowTrading()`) y que la dirección configurada permita el comercio:
   - `BuyOnly` replica el robot de "compra simple de mt4 envolvente".
   - `SellOnly` replica el robot "simple engulf mt4 sell".
   - `Both` permite el comercio bidireccional.
7. Utilice el `TradeVolume` configurado para cada entrada. Si la estrategia está actualmente posicionada en el lado opuesto, cierra la posición y la invierte agregando el tamaño absoluto de la posición a la orden de entrada, coincidiendo con el comportamiento MetaTrader al cambiar de corto a largo (o viceversa).
8. Los niveles opcionales de stop-loss y take-profit se aplican a través de `StartProtection` utilizando unidades basadas en precio. Convierten las distancias de pips en incrementos del precio de los instrumentos para que StockSharp gestione las órdenes de protección de la misma manera que los expertos originales.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | `TimeFrame(15 minutes)` | Tipo de vela e intervalo de agregación utilizados para detectar patrones. |
| `TradeVolume` | `0.01` | Volumen de pedidos por entrada, idéntico a los expertos de MetaTrader. |
| `StopLossPips` | `20` | Distancia de stop-loss expresada en pips. Establezca en `0` para desactivar la orden de protección. |
| `TakeProfitPips` | `20` | Distancia de toma de ganancias expresada en pips. Establezca en `0` para desactivar la orden de protección. |
| `MinBodyPips` | `0` | Cuerpo mínimo de vela (en pips) requerido para un patrón envolvente válido. |
| `MaxBodyPips` | `50` | Cuerpo máximo de vela (en pips) permitido para un patrón envolvente válido. Utilice `0` para quitar el filtro superior. |
| `Direction` | `BuyOnly` | Define qué lado(s) de los asesores originales deben ejecutarse (`BuyOnly`, `SellOnly` o `Both`). |

## Notas prácticas
- El tamaño del pip se adapta automáticamente al instrumento negociado analizando el `PriceStep` y el número de decimales del instrumento. Esto garantiza que los filtros de pips y las órdenes de protección se comporten como las entradas MetaTrader en símbolos de forex de 4 y 5 dígitos.
- Las órdenes de protección se envían solo cuando `StopLossPips` o `TakeProfitPips` son positivas. De lo contrario, la estrategia deja las salidas a la gestión discrecional u otros módulos de automatización.
- Debido a que la estrategia espera velas completamente terminadas, las señales se generan al cierre de cada barra, evitando el repintado dentro de la barra.
- Las llamadas de alto nivel API mantienen la implementación concisa y siguen la directriz del proyecto de preferir componentes StockSharp ya preparados al manejo manual de pedidos.

## Diferencias con el original
- Ambos asesores MetaTrader se combinan en una sola estrategia con un parámetro `Direction` en lugar de dos archivos separados.
- Se agregan ayudantes de registro y gráficos de StockSharp (gráficos comerciales y de velas opcionales) para una mejor visibilidad cuando se ejecuta dentro de las terminales StockSharp.
- La gestión de riesgos utiliza el asistente `StartProtection` de StockSharp, que gestiona internamente las órdenes de limitación de pérdidas y toma de ganancias a través del motor StockSharp. El comportamiento resultante es equivalente a utilizar paradas bruscas en MetaTrader.
