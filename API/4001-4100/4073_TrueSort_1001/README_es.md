# Estrategia TrueSort 1001
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

TrueSort 1001 es un estricto sistema de seguimiento de tendencias que refleja el asesor experto MQL original. La estrategia observa cinco promedios móviles simples y solo actúa cuando permanecen perfectamente ordenados durante tres velas completas consecutivas. Un índice direccional promedio en aumento (ADX) confirma el impulso antes de que se abra cualquier operación. Una vez en el mercado, la posición está protegida por un trailing stop adaptativo medido en pasos de precio y la operación se cierra tan pronto como las medias móviles pierden su alineación.

## Lógica

### Filtro de tendencia e impulso
- Se calculan cinco SMA (10, 20, 50, 100 y 200 períodos de forma predeterminada) en el período de tiempo seleccionado.
- Para configuraciones largas, las SMA rápidas deben estar estrictamente por encima de las más lentas en cada una de las últimas tres velas terminadas: `SMA10 > SMA20 > SMA50 > SMA100 > SMA200`.
- Para configuraciones cortas se requiere el orden opuesto en las mismas tres velas.
- ADX con período `AdxPeriod` debe permanecer por encima de `AdxThreshold` y el valor actual debe ser mayor que la vela anterior, lo que garantiza que la fuerza de la tendencia esté aumentando.

### Condiciones de entrada
1. No hay ninguna posición abierta.
2. Tres velas históricas satisfacen la regla de pedido descrita anteriormente.
3. El filtro ADX pasa.
4. Una orden de mercado de `Volume` lotes se envía inmediatamente al cierre de la vela actual.

### Condiciones de salida
- **Desincronización de media móvil:** cuando la vela actual se cierra y la pila MA ya no está estrictamente ordenada en la dirección de la operación, la posición se liquida.
- **Protección final:** `StopLossPoints` se convierten en distancia de precio absoluta multiplicando por el instrumento `PriceStep`. Para operaciones largas, el stop se inicializa al máximo entre `SMA100` y `Close - distance`. Para cortos es el mínimo entre `SMA100` y `Close + distance`. Después de cada vela, el stop se ajusta al precio, pero nunca se afloja. Si el precio cruza el stop, la posición se cierra en el mercado.

### Notas adicionales
- Todas las decisiones se toman únicamente sobre velas terminadas; se ignoran las velas sin terminar.
- El algoritmo almacena los últimos tres valores SMA internamente para replicar la lógica `shift` del script MQL original sin solicitar el historial del indicador.
- Los valores ADX se procesan a través de `BindEx` y se intenta operar solo cuando la estrategia está en línea y los datos están completamente formados.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `0.1` | Tamaño del pedido en lotes para cada entrada al mercado. |
| `StopLossPoints` | `100` | Distancia del trailing-stop expresada en incrementos del precio del instrumento. `0` desactiva el seguimiento. |
| `Sma10Length` | `10` | Período del SMA más rápido. |
| `Sma20Length` | `20` | Período del segundo SMA. |
| `Sma50Length` | `50` | Período del medio SMA. |
| `Sma100Length` | `100` | Periodo utilizado tanto para la alineación como para la referencia de parada inicial. |
| `Sma200Length` | `200` | El SMA más lento confirma la tendencia a largo plazo. |
| `AdxPeriod` | `14` | Período del indicador ADX. |
| `AdxThreshold` | `25` | Nivel mínimo ADX y condición ascendente necesaria antes de las entradas. |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Serie de velas utilizada para todos los cálculos de indicadores. |

## Detalles de implementación
- El código se basa en la suscripción de vela de alto nivel StockSharp y vincula seis indicadores (cinco SMA y ADX) en una sola canalización.
- Los buffers de historial con longitud tres almacenan los últimos valores de SMA, evitando llamadas a `GetValue()` y manteniendo la paridad exacta con los turnos de MQL.
- Los trailingstops se gestionan manualmente; `StartProtection()` todavía está activado, por lo que la infraestructura estándar está lista en caso de que se necesiten más protecciones.
- Los comentarios dentro del código explican cada paso en inglés para facilitar el mantenimiento.
