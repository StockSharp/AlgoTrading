# Autoadaptable revisado EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Incorporación del MetaTrader 5 asesores expertos `revised_self_adaptive_ea.mq5` al marco estratégico de alto nivel StockSharp.

## Descripción general de la estrategia

El algoritmo escanea una serie de velas configurables y busca configuraciones de reversión envolvente confirmadas por filtros de impulso y tendencia:

* **Detección de patrones**: evalúa la última vela cerrada frente a la anterior. Una configuración alcista requiere un cuerpo verde que se abre por debajo del cierre anterior mientras que la vela anterior es bajista. La lógica del espejo se aplica para configuraciones bajistas. Los cuerpos de las velas se comparan con un promedio móvil para filtrar señales débiles.
* **Filtro de impulso**: un RSI clásico garantiza que las operaciones alcistas solo se activen en territorio de sobreventa y las operaciones bajistas en condiciones de sobrecompra.
* **Filtro de tendencia**: una media móvil simple corta debe coincidir con la dirección comercial. Esto evita que las tendencias fuertes se desvanezcan sin confirmación.
* **Gestión de riesgos**: se calculan niveles de stop-loss y take-profit impulsados por ATR para cada nueva posición. Los trailingstops opcionales siguen movimientos rentables sin reducir nunca la protección. Las posiciones se cierran a la fuerza cuando el precio alcanza los niveles de protección.
* **Diferencial y protección contra riesgos**: las operaciones se omiten siempre que el diferencial actual supere el umbral configurado o cuando el stop basado en ATR arriesgaría más que el porcentaje permitido del precio.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Agregación de velas utilizada para el análisis. El valor predeterminado es barras de una hora. |
| `AverageBodyPeriod` | Número de velas utilizadas para calcular el filtro de tamaño corporal promedio. |
| `MovingAveragePeriod` | Longitud de la media móvil simple que actúa como filtro direccional. |
| `RsiPeriod` | RSI longitud utilizada para la confirmación de sobreventa/sobrecompra. |
| `OversoldLevel` | RSI umbral que se debe alcanzar antes de aceptar una reversión alcista. |
| `OverboughtLevel` | RSI umbral que se debe alcanzar antes de aceptar una reversión bajista. |
| `AtrPeriod` | ATR longitud utilizada para distancias protectoras basadas en la volatilidad. |
| `StopLossAtrMultiplier` | Factor multiplicativo aplicado a ATR para la distancia de stop-loss. |
| `TakeProfitAtrMultiplier` | Factor multiplicativo aplicado a ATR para la distancia de obtención de beneficios. |
| `TrailingStopAtrMultiplier` | ATR distancia mantenida por la lógica del trailing stop. |
| `UseTrailingStop` | Habilita el supervisor de trailing stop. |
| `MaxSpreadPoints` | Spread máximo permitido (expresado en pasos de precio/pips). Las señales se ignoran cuando el mercado es más amplio. |
| `MaxRiskPercent` | Riesgo porcentual máximo aceptable basado en el stop ATR en relación con el precio de entrada. |
| `TradeVolume` | Tamaño de lote base utilizado para órdenes de mercado. |

## Notas de comportamiento

* Las posiciones se aplanan antes de invertir la dirección para reflejar la implementación de MetaTrader.
* Los niveles de parada/toma de protección se vuelven a calcular después de cada llenado utilizando la lectura ATR más reciente.
* El trailing stop solo se mueve en la dirección comercial y se desactiva cuando los datos ATR aún no están disponibles.
* Si la estrategia se ejecuta en un instrumento sin cotizaciones de oferta/demanda confiables, el filtro de diferencial permanecerá inactivo automáticamente.

## Diferencias vs. original MQL

El guión original sólo describía la rutina de detección de señales. En este puerto los elementos faltantes fueron reconstruidos utilizando los parámetros proporcionados:

* Se agregó confirmación de promedio móvil para hacer uso del identificador MA declarado en la fuente MQL.
* Se implementó la lógica de stop-loss, take-profit y trailing stop basada en ATR utilizando el control de volatilidad definido en el experto original.
* Se agregó una protección de porcentaje de riesgo para que las paradas ATR sobredimensionadas se omitan en lugar de ejecutarse a ciegas.
* Se omitieron los elementos de visualización (flechas del gráfico) porque las estrategias StockSharp no dibujan objetos en los gráficos de forma predeterminada.

## Uso

1. Adjunte la estrategia a una cartera y seguridad dentro de Hydra o su host StockSharp personalizado.
2. Asegúrese de que la suscripción a la vela coincida con el plazo previsto (predeterminado: una hora).
3. Ajustar los parámetros de riesgo para reflejar la volatilidad del instrumento.
4. Inicia la estrategia. Se suscribirá automáticamente a velas, calculará indicadores y realizará órdenes de mercado cuando se cumplan las condiciones.
