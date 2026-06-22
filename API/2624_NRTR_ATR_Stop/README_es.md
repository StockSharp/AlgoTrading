# Estrategia NRTR ATR Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia NRTR ATR Stop reproduce el comportamiento del expert de MetaTrader `Exp_NRTR_ATR_STOP` usando la API de alto nivel de StockSharp. Realiza un seguimiento de los niveles NRTR (Non-Repainting Trailing Reverse) construidos desde el Average True Range (ATR). Cuando el precio cruza el trailing stop opuesto, la tendencia cambia y genera una nueva entrada de mercado mientras también cierra cualquier posición abierta en la dirección anterior.

## Lógica del indicador
* Un único **Average True Range** (`AtrPeriod`) se calcula desde la serie de candles suscrita. El valor ATR se multiplica por el `Coefficient` para producir la distancia entre el precio y el nivel de stop actual.
* Se mantienen dos líneas de stop dinámicas:
  * `upper stop` protege las posiciones largas. Sigue al precio desde abajo mientras la tendencia es alcista.
  * `lower stop` protege las posiciones cortas. Sigue al precio desde arriba mientras la tendencia es bajista.
* Cuando el precio cierra más allá del stop opuesto, la tendencia se invierte inmediatamente. El stop en el nuevo lado se inicializa usando el extremo de la vela anterior más/menos la distancia ATR.
* El expert original retrasa la ejecución leyendo el buffer del indicador `SignalBar` velas atrás. La estrategia refleja este comportamiento mediante una cola interna: cada candle finalizado envía su señal a la cola y el motor actúa solo cuando la longitud de la cola supera `SignalBar`.

## Reglas de trading
1. **Señal de compra** – la tendencia calculada cambia de neutral/bajista a alcista. La estrategia opcionalmente cierra cualquier exposición corta y abre una nueva posición larga usando una única orden de mercado cuyo volumen equivale al tamaño de salida requerido más el `Volume` configurado para la nueva entrada larga.
2. **Señal de venta** – la tendencia cambia de neutral/alcista a bajista. La estrategia opcionalmente cierra cualquier exposición larga y abre una nueva posición corta de la misma manera.
3. Las propiedades `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit` y `EnableShortExit` permiten un control preciso sobre qué acciones se ejecutan cuando aparece una señal.
4. Las señales se procesan solo en candles finalizados y mientras la estrategia está en línea y tiene permitido operar.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `AtrPeriod` | Número de candles usados para el cálculo del ATR. |
| `Coefficient` | Multiplicador aplicado al valor ATR al construir los trailing stops. |
| `SignalBar` | Número de candles completamente cerrados a esperar antes de actuar sobre una señal almacenada. Poner en `0` para operar inmediatamente en el candle actual. |
| `CandleType` | Marco temporal de los candles entrantes. |
| `EnableLongEntry` | Permitir abrir posiciones largas en señales de compra. |
| `EnableShortEntry` | Permitir abrir posiciones cortas en señales de venta. |
| `EnableLongExit` | Permitir cerrar posiciones largas cuando ocurre una señal de venta. |
| `EnableShortExit` | Permitir cerrar posiciones cortas cuando ocurre una señal de compra. |

## Notas
* La estrategia se basa únicamente en candles finalizados; los ticks intrabarra son ignorados.
* Las órdenes se envían con `BuyMarket`/`SellMarket`, combinando el cierre de posición y la nueva entrada en una única orden de mercado para simplificar.
* Asegúrese de que la propiedad `Volume` esté configurada con un valor positivo antes de iniciar el trading en vivo o el backtesting.
