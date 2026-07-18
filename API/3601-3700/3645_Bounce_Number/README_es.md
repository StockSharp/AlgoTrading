# Estrategia de número de rebote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de número de rebote** es un puerto StockSharp del indicador MetaTrader `BounceNumber_V0.mq4` / `BounceNumber_V1.mq4`. La herramienta original era un analizador visual que contaba cuántas veces el precio tocaba un canal simétrico antes de salir de él. Esta estrategia de C# recrea el contador de rebotes con el nivel alto API, almacena los resultados en una tabla de distribución e informa cada ciclo completado a través del registro de estrategia. La implementación se mantiene fiel a la lógica de MetaTrader y la adapta al proceso basado en eventos de StockSharp.

A diferencia del indicador original, el puerto se ejecuta como un componente estratégico. Se suscribe a velas terminadas, monitorea los toques de banda y rastrea cuántos golpes alternos ocurren antes de que el precio salga del canal por el doble de su mitad de ancho. Las estadísticas recopiladas se pueden consumir desde la propiedad `BounceDistribution` o desde los mensajes de registro generados.

## como funciona
1. Cuando se inicia la estrategia, valida que el instrumento expone un `PriceStep` distinto de cero. Las entradas basadas en puntos se basan en este valor para convertir MetaTrader "puntos" en distancias de precios decimales.
2. Una suscripción de vela creada a partir de `CandleType` alimenta el analizador de rebotes únicamente con barras completadas.
3. La primera vela entrante define el centro del canal (su precio de cierre). Alrededor de ese centro se crea una banda simétrica cuyo medio ancho es igual a `ChannelPoints * PriceStep`.
4. Cada nueva vela terminada incrementa el contador de ciclos y se evalúa con tres reglas:
   - **Detección de ruptura**: si el rango de la vela cruza `center ± 2 * halfWidth`, el ciclo actual finaliza y se registra su recuento de rebotes.
   - **Toque de banda inferior**: si la vela abarca la banda inferior y el toque anterior no fue también un toque de banda inferior, el contador de rebote aumenta en uno y la dirección cambia a "inferior".
   - **Toque banda superior**: regla simétrica para la banda superior.
5. Si un ciclo dura más velas que `MaxHistoryCandles` (y el parámetro es positivo), el canal se restablece a la fuerza, lo que garantiza que el histograma se actualice incluso cuando el precio se desvíe lateralmente para siempre.
6. En cada ciclo de reinicio, el diccionario de distribución se actualiza y se genera un registro de información que refleja el comportamiento de los contadores de la interfaz original.

La estrategia no realiza ningún pedido por diseño. Debe alojarse junto con otros componentes (paneles de control, interfaz de usuario, exportadores de datos) que consumen las estadísticas de `BounceDistribution`.

## Parámetros
| Nombre | Tipo | Predeterminado | MetaTrader analógico | Descripción |
| --- | --- | --- | --- | --- |
| `MaxHistoryCandles` | `int` | `10000` | `maxbar` entrada | Número máximo de velas permitidas dentro de un ciclo antes de un reinicio forzado. Establezca en `0` para desactivar el reinicio de seguridad. |
| `ChannelPoints` | `int` | `300` | `BPoints` entrada | Medio ancho del canal de rebote expresado en puntos de precio (`PriceStep` múltiplos). |
| `CandleType` | `DataType` | `M1` período de tiempo | `TF` entrada | Serie de velas utilizadas para los cálculos de rebote. |

## Diferencias vs. código MetaTrader
- El histograma se almacena como un diccionario en lugar de objetos de texto en el gráfico. Esto hace que la información sea más fácil de exportar o visualizar en paneles de control StockSharp.
- Las entradas específicas de la interfaz de usuario del indicador (colores, fuentes, botones) se eliminan porque eran cosméticas y no tienen ningún impacto en la lógica analítica.
- El reinicio forzado por `MaxHistoryCandles` ahora es opcional (`0` lo desactiva) y funciona en flujos de datos en vivo, mientras que MetaTrader procesó un bloque histórico finito.
- Todos los mensajes informativos están escritos en inglés hasta `AddInfoLog`, lo que cumple con el requisito de comentarios/registros de código solo en inglés.

## Consejos de uso
- Asegúrese de que la seguridad seleccionada defina `PriceStep`; de lo contrario, la estrategia genera una excepción al inicio porque no se pueden calcular las compensaciones basadas en puntos.
- Combine la estrategia con scripts o widgets de interfaz de usuario personalizados que lean `BounceDistribution` para replicar la cuadrícula de recuentos MetaTrader.
- Utilice valores más pequeños para `ChannelPoints` al analizar el ruido intradiario y valores más grandes para períodos de tiempo más altos o instrumentos volátiles.
- Para emular el escaneo histórico de la versión MQL, inicie la estrategia con `HistoryBuildMode` habilitado en su conector y deje que procese el rango histórico solicitado; la distribución se completará tan pronto como se entreguen las velas rellenas.
