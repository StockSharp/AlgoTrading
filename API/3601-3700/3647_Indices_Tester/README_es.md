# Estrategia del probador de índices
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Probador de Índices** es una adaptación directa del asesor experto MetaTrader 5 "Probador de Índices". El sistema se centra en el comercio de índices intradía, donde se abre una única posición larga durante una ventana de negociación muy estrecha. Las decisiones comerciales se basan únicamente en filtros de tiempo y límites operativos:

- Un único flujo de velas configurable impulsa el reloj interno de la estrategia.
- Sólo se podrán abrir nuevas posiciones entre las horas de inicio y fin de sesión configuradas.
- Se permite un número fijo de operaciones por día, lo que evita reingresos repetidos.
- Todas las posiciones abiertas se cierran forzosamente en un momento de liquidación definido.
- La estrategia opera únicamente en el lado largo, reflejando al asesor experto original.

Esta implementación utiliza StockSharp API de alto nivel, se suscribe a datos de velas con `SubscribeCandles` y maneja decisiones comerciales en la devolución de llamada `ProcessCandle`. No se requieren indicadores, lo que mantiene la lógica ágil y centrada en los controles de tiempo y riesgo.

## Lógica de trading
1. **Restablecimiento diario**: la estrategia realiza un seguimiento del día de negociación actual. Cuando comienza un nuevo día, todos los contadores se reinician, lo que permite una nueva asignación comercial para ese día.
2. **Ventana de entrada**: solo las velas con un tiempo de cierre estrictamente dentro del intervalo `[SessionStart, SessionEnd)` pueden activar entradas. Esto reproduce las comprobaciones `TimeStart` y `TimeEnd` del código original.
3. **Límites de posiciones y operaciones**: las entradas se omiten si la cantidad de operaciones ya abiertas durante el día actual ha alcanzado `DailyTradeLimit`, o si la cantidad de posiciones abiertas simultáneamente excede `MaxOpenPositions`.
4. **Envío de orden**: cuando todas las condiciones se alinean, la estrategia envía una orden de compra de mercado por `TradeVolume` unidades. El contador de operaciones del día se incrementa inmediatamente después del envío de la orden.
5. **Salida forzada**: si una vela se cierra después de `CloseTime` y hay una posición larga activa, la estrategia cierra la posición con una orden de venta de mercado. Esto refleja la lógica del temporizador `ClosePos()` de la implementación MQL.

La combinación del contador de operaciones y el limitador de posición garantiza que el sistema se comporte como un simple programador de operaciones por día de forma predeterminada y, al mismo tiempo, permite el ajuste de parámetros para una actividad más frecuente.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Serie de velas primarias que impulsan el reloj de estrategia (por defecto, velas de 1 minuto). |
| `SessionStart` | Hora del día en la que se permite iniciar nuevas operaciones. |
| `SessionEnd` | Hora del día en la que ya no se permiten nuevas operaciones. |
| `CloseTime` | Hora del día en que se liquida cualquier posición abierta restante. |
| `DailyTradeLimit` | Número máximo de entradas permitidas por día antes de que se suspenda la negociación. |
| `MaxOpenPositions` | Número máximo de posiciones largas abiertas simultáneamente (contadas en unidades comerciales). |
| `TradeVolume` | Volumen de orden de mercado utilizado para cada entrada. |

## Notas y diferencias
- StockSharp no expone tablas de sesión MetaTrader, por lo que la conversión se basa en el tiempo de intercambio de las marcas de tiempo de las velas junto con la protección `IsFormedAndOnlineAndAllowTrading()`.
- El asesor experto original utilizó cronómetros de segundo nivel; este puerto aprovecha los cierres de velas para impulsar tanto el momento de entrada como las salidas forzadas, lo cual es suficiente para ventanas de negociación a nivel de minutos.
- Los recuentos de operaciones se restablecen al comienzo de cada día de negociación detectado a partir de las horas de cierre de las velas, lo que mantiene el comportamiento constante en diferentes zonas horarias siempre que la fuente de la vela coincida con el intercambio deseado.

## Consejos de uso
- Asegúrese de que el `CandleType` configurado coincida con el mercado en el que se negocia para que los filtros de tiempo se alineen con la sesión deseada.
- Aumente `DailyTradeLimit` si se requieren varios intentos por día, por ejemplo, cuando se ejecuta en períodos de tiempo más cortos.
- Establezca `MaxOpenPositions` encima de `1` solo cuando se desee un escalado parcial en posiciones; de lo contrario, mantenga el valor predeterminado para imitar exactamente el script MetaTrader.
