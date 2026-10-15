# Estrategia SilverTrend ColorJFatl Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia SilverTrend ColorJFatl Digit fusiona dos sistemas MetaTrader clásicos en una estrategia unificada de alto nivel de StockSharp. El bloque SilverTrend identifica rupturas direccionales midiendo cuánto viaja el precio dentro de un canal de estilo Donchian corto. El bloque ColorJFatl Digit suaviza el precio con una Media Móvil Jurik (JMA) y evalúa su pendiente después de redondear la salida al número configurado de dígitos. Solo cuando ambos subsistemas coinciden en la dirección la estrategia abre o mantiene una posición. Cuando las señales divergen, la estrategia sale a plano.

El diseño mantiene el espíritu del asesor experto original mientras aprovecha la API de alto nivel de StockSharp: suscripciones de velas, vínculos de indicadores, retrasos de señales basados en colas y ayudantes de dibujo de gráficos. Cada paso está ampliamente documentado para hacer la investigación y optimización posteriores simples.

## Lógica de la estrategia

### 1. Detector de ruptura SilverTrend

* Usa indicadores `Highest` y `Lowest` con `SilverTrendLength + 1` velas para formar el canal de precio reciente.
* El canal se aprieta por el parámetro `SilverTrendRisk`: cuanto mayor sea el valor de riesgo, más cerca quedan los umbrales de ruptura de la línea central del canal (fórmula original `33 - risk`).
* Cuando el precio de cierre rompe por encima del umbral superior ajustado, el bloque SilverTrend reporta una tendencia alcista (`+1`). Cuando rompe por debajo del umbral inferior, el bloque reporta una tendencia bajista (`-1`).
* Un retraso configurable (`SilverTrendSignalBar`) espera `n` velas completamente cerradas antes de que la señal se considere válida, imitando la lógica `SignalBar` de MQL.

### 2. Filtro de confirmación ColorJFatl Digit

* Un `JurikMovingAverage` suaviza el precio aplicado seleccionado por `JmaPriceType`. Se admiten todos los sabores de precio aplicado de MetaTrader (cierre, apertura, mediana, típico, ponderado, simple, cuarto, modos de seguimiento de tendencia y cálculo Demark).
* La salida Jurik se redondea a `JmaRoundDigits`, reproduciendo el comportamiento de indicador "digit" discretizado.
* El signo de la pendiente del JMA redondeado se convierte en la señal de tendencia. Cuando la pendiente es positiva, el filtro emite `+1`; cuando es negativa, `-1`. Las pendientes planas heredan el estado anterior para evitar la alternancia brusca.
* Como con SilverTrend, `JmaSignalBar` retrasa la ejecución, requiriendo que la pendiente se mantenga durante el número solicitado de velas cerradas.

### 3. Ejecución de trades

* **Entrada:**
  * Ir largo cuando tanto SilverTrend como ColorJFatl informan `+1` y no hay exposición larga existente.
  * Ir corto cuando ambos bloques informan `-1` y no hay exposición corta existente.
* **Salida:**
  * Cerrar la posición actual inmediatamente cuando los señales divergen (por ejemplo, un bloque dice `+1`, el otro `-1` o `0`).
  * Las reversiones cierran automáticamente la exposición opuesta antes de abrir la nueva posición para evitar el promediado.
* Las órdenes activas se cancelan antes de las reversiones para mantener el libro limpio.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `SilverTrendCandleType` | Serie de velas usada para calcular el canal de ruptura SilverTrend. Por defecto equivale a H4. |
| `SilverTrendLength` | Longitud de retroceso para el cálculo del canal (parámetro `SSP` en el EA original). |
| `SilverTrendRisk` | Modificador de riesgo que aprieta los umbrales de ruptura (`33 - risk`). Valores más altos reaccionan más rápido pero tienen más falsas señales. |
| `SilverTrendSignalBar` | Número de velas completamente cerradas a esperar antes de aceptar un cambio de color de SilverTrend. |
| `ColorJfatlCandleType` | Serie de velas que alimenta el filtro Jurik. Puede diferir del marco temporal de SilverTrend. |
| `JmaLength` | Longitud de la Media Móvil Jurik. |
| `JmaSignalBar` | Retraso (en barras) antes de actuar sobre los flips de pendiente Jurik. |
| `JmaPriceType` | Modo de precio aplicado para la entrada Jurik (cierre, apertura, mediana, variantes de seguimiento de tendencia, Demark, etc.). |
| `JmaRoundDigits` | Número de decimales usados al redondear la salida Jurik, emulando el indicador discretizado. |

## Notas de implementación

* Los retrasos de señal se implementan con pequeñas colas FIFO en lugar de grandes matrices históricas, asegurando que la estrategia permanezca eficiente en memoria y fiel al Asesor Experto original.
* El código nunca consulta los buffers de indicadores directamente. En cambio, vincula indicadores a través de la API de alto nivel `SubscribeCandles().Bind(...)`, siguiendo las pautas en `AGENTS.md`.
* Los comentarios en línea en inglés explican cada decisión: cuándo se recalculan los umbrales, cómo se calculan las pendientes, por qué se cancelan las órdenes y cómo se impone el consenso.
* El soporte de gráficos está incluido: cuando hay un gráfico disponible, la estrategia dibuja velas de precio, líneas del canal SilverTrend y los propios trades para visualizar las decisiones en vivo.

## Consejos de uso

1. **Mercados y marco temporal:** El sistema original fue diseñado para gráficos H4 de forex. Las criptomonedas y futuros de materias primas con comportamiento de swing claro también funcionan bien. Para mercados más rápidos, reducir `SilverTrendLength` y `JmaLength` con cautela.
2. **Optimización:** Optimizar tanto la longitud de ruptura (`SilverTrendLength`) como la longitud de confirmación (`JmaLength`) juntas — acortar solo una rama usualmente crea señales conflictivas.
3. **Experimentos con precio aplicado:** Probar los modos de precio de seguimiento de tendencia cuando se trabaja con feeds Heikin-Ashi o Renko; a menudo suavizan el ruido mejor que los precios de cierre puros.
4. **Control de riesgo:** Combinar las salidas incorporadas con stops a nivel de portafolio. Dado que ambos módulos tienen un ligero retraso, los picos de volatilidad aún pueden extenderse más allá del canal antes de que el filtro cambie.
5. **Dimensionamiento de posición:** La estrategia deja la gestión de volumen a la propiedad base `Strategy.Volume`. Ajustarlo o integrar las extensiones de gestión monetaria de StockSharp si se requiere piramidación o escalado.

## Ideas para investigación adicional

* Agregar protección de stop-loss y take-profit basada en ATR a través de `StartProtection` una vez que las pruebas confirmen los umbrales preferidos.
* Alimentar velas de marco temporal superior (por ejemplo, Diario) en la confirmación Jurik mientras se mantiene SilverTrend en H4 para introducir un filtro de tendencia.
* Combinar con filtros basados en volumen (Volumen en Balance, divergencia VWAP) para confirmación adicional antes de las entradas.
