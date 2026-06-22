# Estrategia Amstell Gestor de Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port de alto nivel del expert de MetaTrader "exp_Amstell-SL" que ejecuta un grid de promedios bidireccional. La estrategia realiza un seguimiento del precio de fill más reciente en cada lado y emite órdenes de mercado adicionales cuando el precio se aleja suficientemente, mientras liquida el lote abierto una vez que se alcanza una distancia fija de take-profit o stop-loss. La implementación usa las suscripciones de candles y los helpers de órdenes de alto nivel de StockSharp, por lo que se puede conectar a cualquier entorno que proporcione datos de candles para un único instrumento.

La lógica traducida está ligeramente adaptada para el modelo de portafolio neto de StockSharp: los grids largo y corto aún se gestionan por separado, pero no se mantienen al mismo tiempo. El grid largo está activo mientras la posición neta es no negativa, y el grid corto toma el control solo después de que toda la exposición larga ha sido aplanada.

## Cómo funciona

### Flujo de datos y ejecución
- Se suscribe al `CandleType` configurado (predeterminado: candles de marco temporal de 1 minuto) y procesa solo candles finalizados.
- Calcula offsets basados en pips desde el `PriceStep` del instrumento. Si el paso tiene 3 o 5 decimales, se multiplica por 10 para imitar el ajuste de pip de 3/5 dígitos de MetaTrader.
- Todas las operaciones se colocan a través de helpers `BuyMarket`/`SellMarket`; no se usan órdenes pendientes.

### Gestión del lado largo
- Abre la primera posición larga (`OrderVolume`) tan pronto como no haya exposición larga existente y la estrategia no esté cerrando cortos.
- Rastrea el precio de fill largo más reciente y el precio de entrada ponderado por volumen para el lote largo activo.
- Coloca órdenes largas adicionales de tamaño `OrderVolume` siempre que el precio de cierre haya caído al menos `BuyDistancePips` (convertidos a unidades de precio) por debajo del último fill largo.

### Gestión del lado corto
- Una vez que el lote largo está completamente cerrado y la posición neta es no positiva, la estrategia permite entradas cortas.
- Coloca la orden corta inicial cuando no hay exposición corta; shorts adicionales se abren después de que el precio sube `BuyDistancePips * SellDistanceMultiplier` por encima del fill corto anterior.
- Mantiene el precio de fill corto más reciente y el precio de entrada ponderado por volumen para el lote corto activo.

### Reglas de salida
- Para cada dirección, calcula la ganancia no realizada relativa al fill promedio.
- Cierra todo el lote largo con una venta a mercado cuando la ganancia alcanza `TakeProfitPips` pips o el drawdown alcanza `StopLossPips` pips.
- Cierra todo el lote corto con una compra a mercado cuando la ganancia alcanza `TakeProfitPips` pips o el movimiento adverso alcanza `StopLossPips` pips.
- Después de la liquidación, todos los precios y volúmenes en caché se restablecen para que un nuevo grid pueda comenzar en el próximo candle.

### Diferencias con el expert MQL original
- La versión StockSharp opera en cierres de candles en lugar de ticks individuales.
- Los grids largo y corto se ejecutan secuencialmente en lugar de simultáneamente, coincidiendo con el modo de netting predeterminado de StockSharp.
- Todas las distancias de protección se verifican contra el precio de entrada promedio en lugar de cada ticket individualmente, lo que refleja el comportamiento de posición neta agregada.

## Parámetros

| Parámetro | Predeterminado | Rango de optimización | Descripción |
|-----------|---------|--------------------|-------------|
| `OrderVolume` | `0.01` | `0.01` – `0.10` (paso `0.01`) | Cantidad enviada con cada orden de grid. Debe ser positiva. |
| `TakeProfitPips` | `30` | `10` – `150` (paso `10`) | Objetivo de ganancia para el lote activo expresado en pips. |
| `StopLossPips` | `30` | `10` – `150` (paso `10`) | Movimiento adverso máximo antes de abandonar el lote. |
| `BuyDistancePips` | `10` | `5` – `60` (paso `5`) | Caída mínima desde el último fill largo para agregar otra compra. Debe ser menor que TP y SL. |
| `SellDistanceMultiplier` | `10` | `2` – `15` (paso `1`) | Multiplicador aplicado a la distancia larga al espaciar entradas cortas. |
| `CandleType` | Marco temporal de 1 minuto | — | Serie de candles usada para generar señales. |

## Notas de implementación
- `BuyDistancePips` debe ser estrictamente menor que `TakeProfitPips` y `StopLossPips`; la estrategia lanza una excepción al inicio en caso contrario, reproduciendo la validación de MetaTrader.
- El tamaño de pip se deriva del `PriceStep` del instrumento. Ajuste los parámetros si el instrumento usa un tamaño de tick no estándar.
- Todo el estado interno se limpia en `OnReseted`, permitiendo reiniciar la estrategia sin datos residuales del grid.
- No se usa personalización de colores ni registro manual de indicadores, cumpliendo con las directrices de la API de alto nivel en este repositorio.
