# Pausar la negociación en la estrategia de pérdidas consecutivas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de pausar operaciones con pérdidas consecutivas** reproduce la lógica de control de riesgos del asesor experto MetaTrader 4 *"Pausar operaciones con pérdidas consecutivas"*. El script original monitoreaba las operaciones cerradas más recientes, contaba cuántas de ellas terminaron con una ganancia negativa y suspendía nuevas órdenes cuando la racha de pérdidas excedía un límite definido por el usuario en un corto período de tiempo. El puerto StockSharp mantiene ese comportamiento mientras lo envuelve alrededor de un modelo de entrada de impulso mínimo para que el mecanismo de pausa pueda evaluarse dentro de la estrategia independiente.

## como funciona

1. La estrategia se suscribe a velas de marco temporal especificadas por `CandleType`. Cada vez que llega una vela terminada, el precio de cierre se compara con el cierre anterior. Si aumentó, la estrategia intenta una entrada larga; si disminuyó, se considera una entrada corta. Las posiciones salen siempre que una posición alcista se enfrenta a una vela bajista (cierre por debajo de la apertura) o una posición bajista se enfrenta a una vela alcista (cierre por encima de la apertura).
2. Después de cada posición cerrada se inspecciona el beneficio obtenido de la estrategia. Los resultados perdedores ponen en cola su marca de tiempo de cierre en una lista FIFO interna que solo almacena pérdidas consecutivas. Las salidas rentables o de equilibrio borran la lista, del mismo modo que el ciclo MQL abortó una vez que encontró un acuerdo sin pérdidas.
3. Cuando la lista llega a `ConsecutiveLosses` elementos, la estrategia verifica si la diferencia horaria entre la pérdida más antigua y la más reciente está dentro de `WithinMinutes`. Si es así, la negociación se pausa hasta que transcurra `PauseMinutes` desde la última hora de cierre. Durante la pausa no se envían nuevas órdenes de mercado, pero la gestión de posiciones existente continúa funcionando para que el libro pueda aplanarse de forma natural.
4. Una vez que expira la pausa, la lista de pérdidas se borra y las operaciones se reanudan automáticamente. El comportamiento imita las funciones originales `CheckLastNLossDifference` y `lastOrderCloseTime` sin depender de un escaneo persistente del historial de pedidos.

La implementación utiliza las suscripciones de velas de alto nivel de StockSharp (`SubscribeCandles`) y el administrador PnL integrado para monitorear las ganancias obtenidas. Una cola simple (`Queue<DateTimeOffset>`) captura las marcas de tiempo de la racha de pérdidas respetando al mismo tiempo la prohibición del recorrido manual redundante del historial.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | marco de tiempo de 5 minutos | Agregación de velas utilizada para las entradas de impulso simples. |
| `OrderVolume` | `0.1` | Volumen (en lotes/contratos) enviado con cada orden de entrada y salida. |
| `ConsecutiveLosses` | `3` | Número de posiciones perdedoras consecutivas necesarias antes de que se suspendan nuevas operaciones. |
| `WithinMinutes` | `20` | Número máximo de minutos permitidos entre la primera y la última derrota de la racha. Un valor de cero desactiva la verificación de la ventana. |
| `PauseMinutes` | `20` | Duración de la suspensión de la negociación tras la detección de la racha de pérdidas. |

## Notas

- La cola de marcas de tiempo de pérdidas solo se completa cuando la estrategia es plana y acaba de realizar una pérdida. Los cierres parciales o las operaciones rentables no prolongan la racha, evitando falsos positivos.
- El temporizador de pausa se evalúa con respecto a cada vela terminada. Si transcurre `PauseMinutes` mientras la estrategia está inactiva, la siguiente vela desbloquea inmediatamente el comercio.
- Debido a que la versión StockSharp opera en una posición de compensación, la diferencia PnL obtenida se deriva de `PnLManager.RealizedPnL`, reflejando fielmente la búsqueda del historial de MetaTrader sin reprocesar todo el registro de pedidos.
