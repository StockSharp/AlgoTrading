# Estrategia de cambio del régimen Rabbit M2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Rabbit M2 es un asesor experto discrecional originalmente codificado por Peter Byrom para MetaTrader 4. El algoritmo alterna entre
regímenes alcistas y bajistas determinados por promedios móviles exponenciales horarios. Dentro del régimen activo escucha Williams %R
cambios de impulso que son confirmados por el Commodity Channel Index antes de enviar órdenes de mercado. La lógica protectora refleja la
fuente EA adjuntando niveles de stop loss y toma de ganancias de distancia fija y cerrando posiciones siempre que el precio viole el
opuesto al límite del canal Donchian. Un módulo simple de administración de dinero aumenta el tamaño del lote base después de cada lote altamente rentable.
comercio y duplica el objetivo de ganancias requerido para la próxima ampliación.

## Datos e indicadores del mercado.
- **Período de tiempo principal** (predeterminado: velas de 1 minuto) proporciona entradas para Williams %R, CCI y el canal Donchian.
- **Período de tiempo horario** calcula el par rápido (40) y lento (80) EMA que controla la dirección comercial.
- **Williams %R (50)** actúa como activador del impulso cuando cruza las bandas -20/-80.
- **Índice del canal de productos básicos (14)** filtra las operaciones requiriendo lecturas de sobrecompra o sobreventa.
- **Donchian Canal (100)** proporciona salidas de ruptura basadas en el rango alto/bajo anterior.
- **El stop loss estático y la toma de ganancias** se convierten de distancias de puntos (50 por defecto) en compensaciones de precios utilizando el tick de seguridad.
Tamaño, ajustado para instrumentos de 3 y 5 decimales.

## Lógica comercial
### Gestión del régimen
1. Cuando el EMA de 40 períodos en el feed horario cae por debajo del EMA de 80 períodos, todas las posiciones largas se cierran y solo se realizan configuraciones cortas.
están permitidos.
2. Cuando el EMA de 40 períodos supera el EMA de 80 períodos, las posiciones cortas se liquidan y la estrategia solo permite operaciones largas.

### Reglas de entrada
- **Las entradas breves** requieren:
  - Williams %R para pasar de la zona -20..0 al territorio de sobreventa (< -20).
  - CCI para exceder el umbral de venta configurable (predeterminado 101).
  - Exposición corta neta por debajo del límite `MaxTrades` (cada operación añade una unidad de volumen base).
- **Las entradas largas** requieren:
  - Williams %R para salir de la zona -100..-80 e imprimir un valor superior a -80.
  - CCI caiga por debajo del umbral de compra (99 predeterminado).
  - Exposición larga neta por debajo del límite de `MaxTrades`.

Cada pedido se envía con el volumen base actual. El puerto StockSharp utiliza posiciones de compensación, por lo que las señales repetidas simplemente aumentan
la exposición neta hasta alcanzar el límite configurado.

### reglas de salida
1. Los niveles de stop loss y takeprofit se controlan en cada vela terminada. Una vez que el precio cruza un nivel, la posición es
cerrado con una orden de mercado.
2. Independientemente de los niveles de parada/objetivo, una posición larga se cierra cuando el cierre cae por debajo de la banda inferior Donchian anterior;
un corto se cierra cuando el cierre se eleva por encima de la banda superior Donchian anterior.
3. Un cambio de régimen causado por el cruce horario EMA liquida inmediatamente las posiciones que se oponen a la nueva dirección.

### gestión del dinero
- El tamaño del pedido base comienza desde `InitialVolume` (predeterminado 0,01) y respeta el paso de volumen de seguridad, mínimo y máximo.
- Después de cada beneficio obtenido superior a `BigWinTarget` (predeterminado 15 unidades monetarias), el volumen base aumenta en
`VolumeIncrement` (predeterminado 0.01) y el umbral de ganancias se duplica, coincidiendo con el comportamiento en cascada de la versión MetaTrader.
- Cuando la estrategia es plana, cualquier marcador de posición de parada/toma pendiente se restablece para evitar valores obsoletos.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CciSellLevel` | 101 | Valor mínimo CCI que confirma una señal corta. |
| `CciBuyLevel` | 99 | Valor máximo CCI que confirma una señal larga. |
| `CciPeriod` | 14 | Longitud retrospectiva del índice de canales de productos básicos. |
| `DonchianPeriod` | 100 | Donchian período de canal utilizado para salidas de ruptura. |
| `MaxTrades` | 1 | Número máximo de unidades de volumen base permitidas en la posición neta. |
| `BigWinTarget` | 15 | Se requiere beneficio realizado antes de aumentar el volumen base. |
| `VolumeIncrement` | 0,01 | Volumen adicional añadido después de una victoria de clasificación. |
| `WprPeriod` | 50 | Williams Período de cálculo %R. |
| `FastEmaPeriod` | 40 | Período EMA rápida en el feed de tendencias por hora. |
| `SlowEmaPeriod` | 80 | Período EMA lento en el feed de tendencias por hora. |
| `TakeProfitPoints` | 50 | Tome la distancia de ganancias en puntos de precio. |
| `StopLossPoints` | 50 | Distancia de stop loss en puntos de precio. |
| `InitialVolume` | 0,01 | Tamaño inicial del pedido base. |
| `CandleType` | velas de 1 minuto | Marco de tiempo principal utilizado para los cálculos de impulso y salida. |

## Notas de implementación
- Los niveles de parada de pérdidas y toma de ganancias se evalúan dentro de la estrategia en lugar de enviarse como órdenes separadas para replicar la
comportamiento de los parámetros `OrderSend` de MetaTrader.
- Los ajustes de volumen se basan en el PnL realizado informado por StockSharp. Asegúrese de que la estrategia reciba confirmaciones comerciales del
conexión del intermediario para que se active la lógica de escalado.
- El método auxiliar `CalculatePriceOffset` infla el tamaño en puntos para símbolos de divisas de 3 y 5 decimales, reproduciendo el `Point`
constante desde la plataforma original.
