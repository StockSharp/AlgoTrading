# Segunda estrategia más fácil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La segunda estrategia más fácil es el puerto StockSharp del experto MetaTrader *Second_Easiest.mq4*. El robot original escanea el
vela diaria de la sesión de negociación actual y abre una única posición intradiaria una vez que el precio demuestra que tiene una tendencia alejándose del
El día está abierto. Cuando el mercado cierra, el experto liquida cualquier exposición y se prepara para la siguiente sesión. El StockSharp
La versión conserva este comportamiento de ruptura intradía mientras aprovecha el API de alto nivel del marco para velas.
suscripciones, gestión de pedidos y seguimiento de posiciones.

A diferencia de las estrategias de impulso que requieren múltiples indicadores, Second Easyest solo necesita la apertura, el alza y la baja del
día actual. Esto lo hace muy liviano y al mismo tiempo reacciona a los primeros signos de convicción direccional. El código se mantiene
una posición a la vez y nunca retrocede inmediatamente; la nueva operación se puede abrir sólo después de que se haya cerrado la anterior.

## Lógica comercial
1. Suscríbase a la serie de velas intradía definida por `CandleType`. El valor predeterminado es un período de tiempo de un minuto, lo que proporciona una anticipación
vista de los extremos diarios sin dejar de ser compatible con la lógica diaria del EA original.
2. Para cada vela terminada, actualice el registro en memoria de los precios de apertura, máximo y mínimo de la sesión. La primera vela procesada.
en un nuevo día de negociación se definen los tres valores; Las velas posteriores expanden solo el máximo o el mínimo cada vez que se alcanza un nuevo extremo.
3. Ignore las nuevas configuraciones una vez que el reloj llegue a `EntryCutoffHour`. El código MetaTrader deja de abrir operaciones a las 16:00 hora del servidor y
el puerto sigue la misma regla.
4. Se permite una posición larga solo cuando el cierre actual se negocia por encima de la apertura diaria **y** la distancia entre la apertura y la
El mínimo diario supera `RangePointsThreshold`. Esto reproduce las condiciones "Oferta > abierta" y "apertura - baja > 15 puntos" de MQL.
5. Se permite una posición corta sólo cuando el cierre actual se sitúa por debajo de la apertura diaria **y** la distancia entre el máximo diario y
la apertura supera el mismo umbral.
6. Siempre que aparezca una señal de entrada y no haya ninguna posición abierta, envíe una orden de mercado utilizando `TradeVolume` lotes. Los métodos auxiliares de
la clase base `Strategy` se encarga de seleccionar el lado correcto.
7. Después de que el mercado alcance `MarketCloseHour`, reduzca cualquier exposición existente llamando a `ClosePosition()`. No se realizan nuevas operaciones
después de este corte hasta que comience la próxima sesión.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | marco de tiempo de 1 minuto | Velas intradiarias primarias que impulsan la lógica de entrada y salida. |
| `TradeVolume` | `decimal` | `1` | Tamaño de lote utilizado para cada orden de mercado. |
| `EntryCutoffHour` | `int` | `16` | Hora (0-23) tras la cual la estrategia se niega a abrir nuevas posiciones. |
| `MarketCloseHour` | `int` | `20` | Hora (0-23) en la que se cierra con fuerza cualquier posición abierta. |
| `RangePointsThreshold` | `decimal` | `15` | Distancia mínima, expresada en puntos del broker, entre la apertura diaria y el extremo más cercano. |

## Diferencias vs. la versión MetaTrader
- La versión StockSharp rastrea las posiciones de forma neta. El comportamiento es idéntico a la lógica de orden único original.
porque solo se puede abrir una operación a la vez y la posición se aplana antes de que se evalúen nuevas entradas.
- MetaTrader recupera las llamadas abiertas, altas y bajas en ejecución hasta `iOpen/iHigh/iLow` en el período de tiempo diario. El puerto se reconstruye
la misma información de las velas intradía, evitando llamadas prohibidas a indicadores y garantizando que los datos permanezcan disponibles incluso cuando
El feed de corretaje no proporciona barras diarias.
- El cierre de la orden se realiza a través de `ClosePosition()` en lugar de recorrer los identificadores de ticket. El resultado final es el mismo:
La exposición abierta se elimina tan pronto como se alcanza la hora de cierre configurada.
- Si no se proporciona el `PriceStep` del valor, la conversión trata el `RangePointsThreshold` como una distancia de precio absoluta.
Este respaldo de seguridad mantiene el sistema operativo en instrumentos que informan precios sin metadatos de pasos.

## Notas de uso
- `Volume` está establecido en `TradeVolume` en `OnStarted`, por lo que cambiar el parámetro afecta inmediatamente a los pedidos posteriores sin
modificando el resto del código.
- Al elegir un `CandleType` diferente, asegúrese de que aún proporcione suficiente granularidad para realizar un seguimiento de la apertura/máximo/mínimo intradiario.
con precisión. Por ejemplo, las velas de cinco minutos funcionan bien, pero las barras de una hora pueden retrasar la detección de los extremos diarios.
- Aumente `RangePointsThreshold` para filtrar sesiones de baja volatilidad. Disminuirlo permite que la estrategia se active incluso cuando
el rango temprano es pequeño.
- Debido a que el algoritmo cierra todas las posiciones al final del día, no requiere margen nocturno. Corredores que hacen cumplir
Las pausas de sesión también restablecerán automáticamente los contadores de rango interno cuando se reanude la negociación.
