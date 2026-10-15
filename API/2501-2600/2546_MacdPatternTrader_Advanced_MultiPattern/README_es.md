# Estrategia MacdPatternTrader Avanzado MultiPatrón
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MacdPatternTrader es una conversión de alto nivel de StockSharp del asesor experto MQL original *MacdPatternTraderAll*. El sistema escucha velas completadas y evalúa seis patrones de entrada independientes basados en MACD. Cada patrón usa sus propias medias móviles exponenciales rápidas y lentas más niveles de umbral dedicados para reconocer estructuras de reversión y continuación en la línea principal del MACD. Las señales pueden llegar simultáneamente y cada una envía una orden de mercado dimensionada por el volumen martingala actual.

La estrategia complementa la lógica de entrada con gestión de riesgo adaptativa. Los precios de stop-loss se calculan a partir de máximos o mínimos recientes con un offset, mientras que los objetivos de take-profit se extienden a través de bloques de historia sucesivos de la misma manera que la implementación MQL. Las posiciones abiertas se gestionan activamente mediante salidas parciales basadas en filtros EMA/SMA y un umbral de ganancia no realizada. Después de cada cierre plano, el multiplicador martingala restablece o duplica el tamaño del lote dependiendo del resultado realizado.

## Reglas de trading
1. **Patrón 1 – Reversión por umbral**
   * Rastrea cuando la línea principal del MACD sube por encima de un umbral superior, luego gira hacia abajo mientras permanece positiva.
   * Refleja el comportamiento para el umbral inferior cuando el MACD se recupera del territorio negativo.
2. **Patrón 2 – Rebote de nivel cero**
   * Requiere una fase MACD positiva, luego un gancho bajista bajo la línea cero antes de vender.
   * Usa la lógica simétrica para ganchos alcistas sobre cero para comprar.
3. **Patrón 3 – Secuencia multietapa**
   * Reproduce el reconocimiento de cresta y valle en tres etapas del código MQL fuente usando banderas anidadas y pares de umbrales.
   * Restablece los contadores auxiliares (`bars_bup`) después de cada operación ejecutada.
4. **Patrón 4 – Pico/valle local**
   * Espera máximos o mínimos locales de MACD en relación con las dos barras anteriores para configurar señales cortas y largas respectivamente.
5. **Patrón 5 – Rompimiento de banda neutral**
   * Busca entradas cortas después de caer por debajo de una banda neutral e inmediatamente volver por debajo de un límite bajista.
   * Busca entradas largas después de moverse por encima de la banda neutral y saltar sobre un límite alcista.
6. **Patrón 6 – Contador de barras consecutivas**
   * Cuenta el número de barras por encima o por debajo de los umbrales configurados y solo se activa cuando el contador supera el valor `TriggerBars` mientras permanece por debajo del límite `MaxBars`.

## Gestión de riesgo y gestión de operaciones
* **Stop-loss** – Determinado por el precio más alto (para operaciones cortas) o más bajo (para operaciones largas) durante las últimas velas `StopLossBars` más el offset configurado traducido a unidades de paso de precio.
* **Take-profit** – Busca segmentos de historia consecutivos de velas `TakeProfitBars`, exactamente como los bucles `iLowest`/`iHighest` anidados en la versión MQL. El objetivo se extiende mientras el siguiente segmento produce un valor más extremo.
* **Salidas parciales** – Una vez que la ganancia no realizada supera cinco unidades monetarias (aproximada por diferencia de precio × volumen de posición) y los filtros EMA/SMA están de acuerdo, la estrategia cierra un tercio del volumen abierto, luego la mitad del resto.
* **Control de lote martingala** – Después de un cierre plano la estrategia restablece el lote a `InitialVolume` cuando la operación cerrada ganó dinero; de lo contrario, el volumen se duplica (si `UseMartingale` está habilitado).
* **Filtro de tiempo** – Cuando `UseTimeFilter` está habilitado la estrategia solo evalúa entradas dentro de la ventana `(StartTime, StopTime)`. Los stops todavía se verifican en cada vela terminada.

## Parámetros
| Grupo | Nombre | Descripción |
| --- | --- | --- |
| Patrón 1 | `Pattern1Enabled` | Habilita el primer patrón MACD. |
| Patrón 1 | `Pattern1StopLossBars`, `Pattern1TakeProfitBars`, `Pattern1Offset` | Configuraciones de lookback y offset de stop-loss/take-profit. |
| Patrón 1 | `Pattern1Slow`, `Pattern1Fast` | Longitudes EMA lentas y rápidas para el cálculo MACD. |
| Patrón 1 | `Pattern1MaxThreshold`, `Pattern1MinThreshold` | Umbrales MACD superiores e inferiores. |
| Patrón 2 | La misma estructura que el patrón 1 con sus propios valores. |
| Patrón 3 | Agrega umbrales adicionales `Pattern3MaxLowThreshold` y `Pattern3MinHighThreshold` para reproducir el reconocimiento de cresta/valle por niveles. |
| Patrón 4 | Incluye `Pattern4AdditionalBars` (conservado para compatibilidad con el código original). |
| Patrón 5 | Usa umbrales neutrales para la detección de rompimiento de banda. |
| Patrón 6 | Agrega `Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6TriggerBars` para gestionar la lógica del contador de barras. |
| Gestión | `EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4` | Medias móviles para filtros de salida parcial. |
| General | `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`, `CandleType` | Controles de comportamiento global. |

## Notas
* La conversión mantiene la estructura lógica original, incluyendo la búsqueda segmentada de take-profit y las reglas de restablecimiento martingala.
* Las salidas parciales basadas en ganancias usan una aproximación porque el API de alto nivel de StockSharp no expone valores de ganancia de terminal bruta por posición; en su lugar se usa diferencia de precio × volumen.
* `Pattern4AdditionalBars` se conserva para compatibilidad aunque el código MQL original nunca lo referenció directamente.
* Los stops y take-profits se evalúan en velas cerradas porque StockSharp no adjunta órdenes protectoras automáticamente en el API de alto nivel.
