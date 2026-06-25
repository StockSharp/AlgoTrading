# ZigZag EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
La estrategia replica la lógica original del MT5 "ZigZag EA" esperando tres puntos de oscilación ZigZag consecutivos y colocando dos órdenes de stop de ruptura alrededor del rango de oscilación anterior. La conversión utiliza la API de alto nivel de StockSharp y trabaja con velas terminadas. Las dos últimas oscilaciones completadas definen un corredor de trading, mientras que la oscilación más reciente ("room 0" en la versión MQL) debe permanecer dentro de ese corredor antes de que la estrategia se arme con órdenes pendientes. El enfoque es simétrico: prepara tanto órdenes buy-stop como sell-stop y permite que el mercado decida la dirección de la ruptura.

## Indicadores y datos de mercado
* **Highest / Lowest:** StockSharp no expone directamente el indicador ZigZag de MT, por lo tanto la conversión imita el comportamiento de ZigZag rastreando los valores más altos y más bajos consecutivos sobre la profundidad seleccionada. Los cambios de dirección actualizan los buffers de oscilación internos exactamente como el EA original leyendo el buffer ZigZag.
* **Velas:** la estrategia se suscribe a un tipo de vela configurable (predeterminado: marco temporal de 1 minuto) y trabaja solo con velas terminadas para mantener la compatibilidad con backtesting y trading real.

## Lógica de trading
1. Recopilar los últimos tres valores de oscilación. Los dos valores anteriores determinan el corredor (`high`/`low`), y el último valor debe permanecer dentro del corredor con un pequeño buffer definido por el nivel de stop del broker.
2. Aplicar límites de tamaño del corredor (`MinCorridorPips` y `MaxCorridorPips`). Los corredores demasiado estrechos se ignoran para evitar ruido, mientras que los corredores demasiado amplios se filtran para evitar stops enormes.
3. Una vez que el corredor es válido y no hay posición abierta, colocar órdenes pendientes simétricas:
   * **Buy stop** en `high + EntryOffsetPips`.
   * **Sell stop** en `low - EntryOffsetPips`.
4. Los stops y objetivos se calculan a partir de ratios de Fibonacci exactamente como en la implementación MQL: `FiboStopLoss` multiplica la altura del corredor y `FiboTakeProfit` resta el corredor de la proyección de Fibonacci seleccionada. Los precios se redondean al tamaño del tick del instrumento para evitar rechazos.
5. Cuando una orden pendiente se activa, la orden pendiente restante se cancela y el stop-loss y take-profit protectores se registran inmediatamente. La lógica de trailing opcional ajusta el stop cuando el precio avanza `TrailingStepPips` más allá de la distancia de trailing.
6. La estrategia se cierra y se rearma automáticamente cuando la posición vuelve a cero.

## Gestión de riesgo y órdenes
* Las órdenes protectoras de stop y objetivo son órdenes live stop/limit, por lo que el broker controla la ejecución y los gaps se manejan naturalmente.
* La lógica de trailing stop se ha tomado del EA: se activa después de que el beneficio supera `TrailingStopPips + TrailingStepPips` y luego re-registra el stop cada vez que la distancia aumenta en al menos un paso de trailing.
* La estrategia utiliza el parámetro base `Volume` de la clase `Strategy` de StockSharp. Los bloques de gestión monetaria de la versión MQL (lote fijo vs. porcentaje de riesgo) se omiten intencionalmente porque el dimensionamiento de posición es generalmente específico del broker en StockSharp.

## Filtro de sesión
* El trading solo está permitido entre `StartHour:StartMinute` y `StopHour:StopMinute`. Si el tiempo de stop es anterior al tiempo de inicio, la estrategia lo trata como una sesión nocturna y permite el trading a través de la medianoche.
* Las órdenes pendientes se cancelan siempre que la sesión está cerrada, reflejando el comportamiento MQL que eliminaba órdenes fuera de la ventana permitida.

## Parámetros
| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `CandleType` | Serie de velas usada para análisis. | Velas de 1 minuto |
| `ZigZagDepth` | Número de velas para la detección de oscilaciones. | 12 |
| `EntryOffsetPips` | Offset agregado por encima/debajo del corredor. | 5 |
| `MinCorridorPips` | Altura mínima del corredor para validar una configuración. | 20 |
| `MaxCorridorPips` | Altura máxima del corredor permitida. | 100 |
| `FiboStopLoss` | Nivel de Fibonacci usado para calcular la distancia del stop-loss. | 61.8% |
| `FiboTakeProfit` | Nivel de Fibonacci usado para el objetivo de beneficio. | 161.8% |
| `StartHour` / `StartMinute` | Inicio de la ventana de trading. | 00:01 |
| `StopHour` / `StopMinute` | Fin de la ventana de trading. | 23:59 |
| `TrailingStopPips` | Distancia usada por el trailing stop. | 5 |
| `TrailingStepPips` | Mejora mínima requerida para mover el trailing stop. | 5 |
| `DrawCorridorLevels` | Si está habilitado, la estrategia dibuja un marcador de corredor vertical en el gráfico como referencia. | `false` |

## Notas de implementación
* Los valores en pips se calculan a partir del tamaño del tick del instrumento. Los instrumentos con 3 o 5 decimales multiplican automáticamente el tick por 10, replicando la lógica de "punto ajustado" usada en el EA.
* El código utiliza métodos auxiliares de alto nivel como `BuyStop`, `SellStop`, `SellLimit` y `BuyLimit`, en línea con las pautas del proyecto.
* Los comentarios se mantienen en inglés para cumplir con los requisitos del repositorio, mientras que la descripción detallada se proporciona en varios idiomas en los archivos README.
* No se crea un port en Python; la carpeta contiene solo la implementación en C# según lo solicitado.
