# Estrategia totalmente húmeda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Full Damp es un sistema de inversión de tendencias construido alrededor de un conjunto triple de Bollinger bandas combinadas con un filtro de confirmación de índice de fuerza relativa (RSI). La estrategia espera picos de precios más allá de la banda Bollinger más amplia para detectar un posible agotamiento. Una lectura reciente de sobreventa o sobrecompra de RSI valida la señal antes de que se active la operación cuando el precio regresa dentro de la banda de ancho medio. Una vez posicionadas, las salidas se gestionan con toma de ganancias parcial, ajustes de stop dinámicos y reglas de seguimiento basadas en Bollinger.

## Lógica de trading

1. **Detección de señal**
   * Las configuraciones largas aparecen cuando el mínimo de la vela cierra en o por debajo de la banda inferior de un conjunto Bollinger con ancho 3. Las configuraciones cortas ocurren cuando el máximo de la vela alcanza la banda superior del mismo conjunto.
   * El RSI debe haber alcanzado el umbral de sobreventa (largo) o sobrecompra (corto) dentro de las últimas velas *Lookback Bars*. Esta condición se monitorea continuamente, por lo que un nuevo extremo RSI actualiza la cuenta regresiva.
2. **Activador de entrada**
   * Una posición larga se abre una vez que el precio cierra por encima de la banda inferior del conjunto medio Bollinger (ancho 2), siempre que no haya ninguna posición abierta.
   * Se abre una posición corta después de que el precio cierra por debajo de la banda superior del conjunto medio Bollinger.
   * Los niveles iniciales de stop-loss están anclados al mínimo más bajo (para largos) o al máximo más alto (para cortos) visto desde la vela de señal, ampliado por el desplazamiento del punto configurable.
3. **Gestión de posiciones**
   * Cuando el mercado alcanza una ganancia igual al riesgo inicial, la mitad de la posición se cierra y el stop-loss se mueve al punto de equilibrio.
   * El volumen restante se sale si el máximo de la vela (para largos) o el mínimo (para cortos) cruza la banda media Bollinger en la dirección opuesta.
   * Si el precio regresa al nivel stop antes de alcanzar el objetivo de ganancias, se cierra toda la posición.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Fuente de datos de velas utilizada para análisis y ejecución. | Velas por hora |
| `BollingerPeriod1` | Periodo de las Bandas estrechas Bollinger (ancho = 1). | 20 |
| `BollingerPeriod2` | Período del medio Bollinger Bandas (ancho = 2). | 20 |
| `BollingerPeriod3` | Periodo de las Bandas anchas Bollinger (ancho = 3). | 20 |
| `RsiPeriod` | RSI período utilizado para la confirmación de la señal. | 14 |
| `LookbackBars` | Número de velas completadas dentro de las cuales RSI debe alcanzar los niveles extremos. | 6 |
| `StopOffsetPoints` | Colchón adicional (en puntos de precio) agregado al nivel inicial de stop-loss. | 10 |
| `Volume` | Volumen de órdenes heredado de la estrategia base. | 1 |

## Notas

* Los umbrales de RSI se fijan en 30 para señales largas y 70 para señales cortas para imitar la lógica MQL original.
* La estrategia utiliza el StockSharp API de alto nivel: los indicadores están vinculados a la suscripción de velas, la gestión comercial utiliza órdenes de mercado y la lógica de protección se maneja internamente sin sondeo manual del valor del indicador.
* Las salidas parciales y los ajustes de parada se ejecutan al cerrar la vela para mantener el comportamiento alineado con la implementación original.
