# Estrategia Nirvaman Imax
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Nirvaman Imax es una conversión directa del MetaTrader 4 asesor experto `NirvamanImax.mq4` incluido con los indicadores personalizados HA, Moving Averages2 e iMAX3alert. La implementación StockSharp mantiene la idea original de combinar Heikin-Ashi velas con un detector de tendencias de dos fases y un filtro de línea base EMA mientras adopta el nivel alto API. La estrategia funciona con un único instrumento y marco de tiempo y cierra operaciones automáticamente después de un período de tenencia configurable.

## Indicadores y filtros
- **Heikin-Ashi velas**: reproduce el indicador HA original y clasifica las velas como alcistas o bajistas comparando los valores de apertura y cierre de Heikin.
- **Cruce rápido/EMA lenta**: reemplaza la salida bifásica MT4 `iMAX3alert1`. Aparece una señal alcista cuando el EMA rápido cruza por encima del EMA lento; se produce una señal bajista en el cruce opuesto.
- **EMA filtro de tendencias**: refleja el búfer `Moving Averages2` EMA y actúa como línea de base. Sólo se permiten operaciones largas por encima del filtro y operaciones cortas por debajo del mismo.
- **Filtro de tiempo**: omite cualquier vela cuya hora se encuentre dentro de la ventana prohibida definida por `NoTradeStartHour` y `NoTradeEndHour` (la ventana admite un cambio de zona horaria alrededor de la medianoche y un cambio de zona horaria del corredor).
- **Salida programada**: cada posición se fuerza a cerrar después de que transcurra `CloseAfter`, reproduciendo la lógica `tiempoCierre` de la versión MQL.
- **Paradas y objetivos**: el límite de pérdidas y la toma de ganancias se aplican en incrementos de precio derivados del tamaño del tick del instrumento. Establecer cualquiera de ellos en `0` deshabilita la protección correspondiente.

## Reglas comerciales
1. Espere hasta que se formen Heikin-Ashi, EMA rápida, EMA lenta y el filtro EMA y esté disponible un cierre de vela anterior.
2. Rechace la señal si el tiempo de la vela está dentro de la ventana comercial restringida.
3. Entrada larga:
   - El EMA rápido cruza por encima del EMA lento en la vela actual.
   - El cierre de Heikin-Ashi está por encima de su apertura (cuerpo alcista).
   - El cierre de la vela anterior está por encima del filtro EMA.
4. Entrada corta:
   - El EMA rápida cruza por debajo del EMA lenta en la vela actual.
   - El cierre de Heikin-Ashi está por debajo de su apertura (cuerpo bajista).
   - El cierre de la vela anterior está por debajo del filtro EMA.
5. Reglas de salida:
   - Los niveles de stop loss o takeprofit se ven afectados por el rango de velas.
   - Se superó la duración máxima de la posición `CloseAfter`.
   - La protección manual activada mediante `StartProtection()` cierra la posición cuando el motor lo solicita.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Volumen de orden de mercado base. | `0.1` |
| `CandleType` | Marco de tiempo de vela utilizado para cada indicador y señal. | `30m` período de tiempo |
| `FastTrendLength` | Duración del EMA rápida que emula la fase azul del iMAX. | `10` |
| `SlowTrendLength` | Duración del EMA lenta que emula la fase roja del iMAX. | `21` |
| `FilterLength` | EMA período para el filtro de referencia (equivalente a medias móviles2). | `13` |
| `StopLoss` | Distancia de parada protectora en pasos de precios; `0` desactiva la parada. | `50` |
| `TakeProfit` | Distancia objetivo de beneficios en pasos de precios; `0` desactiva el objetivo. | `100` |
| `CloseAfter` | Tiempo máximo de espera antes de que se fuerce el cierre de la posición. | `15000 s` |
| `NoTradeStartHour` | Hora (0–23) que marca el comienzo de la ventana de no comercio. | `22` |
| `NoTradeEndHour` | Hora (0–23) que marca el final de la ventana de no comercio. | `2` |
| `BrokerTimeOffset` | Desplazamiento de zona horaria del agente (horas) aplicado antes del filtro de hora. | `0` |

## Notas de conversión
- El indicador MT4 `iMAX3alert1` expone dos buffers codificados por colores. Su cruce se traduce en un cruce EMA rápido/lento, que conserva la lógica de entrada original basada en eventos.
- El indicador Moving Averages2 se ejecutó en modo EMA con una longitud predeterminada de 13. La versión StockSharp reutiliza un estándar `ExponentialMovingAverage` con el mismo valor predeterminado.
- La gestión del ciclo de vida de la posición refleja el script MQL: la posición se cierra en el tiempo de espera antes de que se puedan evaluar nuevas entradas y no se agregó ninguna lógica de trailing stop adicional.

## Consejos de uso
1. Adjunte la estrategia a un tablero/seguridad y establezca el `CandleType` deseado antes de iniciarla.
2. Ajuste `TradeVolume`, `StopLoss`, `TakeProfit` y `CloseAfter` para que coincidan con la volatilidad y la tolerancia al riesgo del instrumento.
3. Optimice los períodos EMA si necesita aproximarse al comportamiento del ajuste del iMAX original para un nuevo mercado.
4. Combínelo con controles de riesgo de nivel superior (protección de cartera, control de sesión) cuando ejecute múltiples instancias.
