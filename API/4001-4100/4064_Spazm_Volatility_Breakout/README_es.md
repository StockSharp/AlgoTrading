# Estrategia de ruptura de volatilidad de Spazm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Conversión del MetaTrader 4 asesor experto **Spazm (8683)** al StockSharp alto nivel API.
- Negocia rupturas adaptativas comparando el último cierre con sobres de volatilidad alrededor del máximo y mínimo más recientes.
- Mantiene anotaciones de gráficos opcionales que unen pivotes alcistas y bajistas consecutivos como la visualización original MQL.

## Preparación de datos
1. La estrategia se suscribe a la serie de velas especificada por el parámetro `CandleType` para el valor activo.
2. Cada vela terminada proporciona la muestra del rango bruto utilizado para la estimación de la volatilidad:
   - De forma predeterminada, el rango es igual a `High - Low`.
   - Cuando `UseOpenCloseRange` está habilitado, se utiliza el tamaño absoluto del cuerpo `|Open - Close|`.
3. La muestra de rango se convierte en incrementos de precios utilizando el instrumento `PriceStep` para que la lógica permanezca invariante entre los símbolos.
4. El indicador definido por `UseWeightedVolatility` procesa la secuencia de muestras de rango:
   - Deshabilitado → media móvil simple con longitud `VolatilityPeriod`.
   - Habilitado → media móvil ponderada lineal (más peso para velas recientes).
5. El rango suavizado (expresado en pasos) se multiplica por `VolatilityMultiplier` y finalmente se reduce a unidades de precio. El valor resultante es el umbral de ruptura adaptativo aplicado a ambos lados del mercado.
6. Durante la fase de calentamiento, la estrategia también registra los máximos y mínimos extremos más recientes junto con sus marcas de tiempo. Una vez que se procesan las velas `VolatilityPeriod * 3`, el momento relativo de esos extremos determina la dirección de la tendencia inicial.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `1` | Volumen de orden enviado cada vez que la estrategia abre o revierte una posición. |
| `VolatilityMultiplier` | `5` | Multiplicador aplicado a la volatilidad promedio para construir la distancia de ruptura. |
| `VolatilityPeriod` | `24` | Número de velas utilizadas tanto para el estimador de volatilidad como para sembrar los extremos de oscilación iniciales. |
| `UseWeightedVolatility` | `false` | Cambia el estimador de volatilidad de un promedio móvil ponderado simple a uno lineal. |
| `UseOpenCloseRange` | `false` | Utiliza el movimiento absoluto de apertura y cierre en lugar del rango alto-bajo al medir la volatilidad. |
| `StopLossMultiplier` | `0` | Multiplicador aplicado al umbral de ruptura para calcular una distancia de parada protectora. Se aplica un mínimo de tres niveles de precios. Establezca en `0` para deshabilitar las paradas. |
| `DrawSwingLines` | `true` | Cuando está habilitada, la estrategia traza una línea entre los últimos pivotes alcistas y bajistas, imitando los objetos MQL. |
| `CandleType` | `4 hour time frame` | Tipo de vela (período de tiempo u otro tipo de datos) que alimenta los cálculos. |

## Lógica de trading
1. **Inicialización**
   - Mientras se procesan las primeras velas `VolatilityPeriod * 3`, la estrategia actualiza `_highestPrice`, `_lowestPrice`, `_highestTime` y `_lowestTime` para capturar los últimos extremos.
   - Después de que lleguen suficientes velas, el más reciente de los dos extremos define la tendencia inicial: si el último mínimo es más nuevo que el último máximo, la estrategia comienza en modo alcista; de lo contrario, comienza en modo bajista.
   - Los extremos también se almacenan como el primer par de anclajes del swing para que las líneas del gráfico se puedan dibujar inmediatamente después del calentamiento.
2. **Seguimiento de la volatilidad**
   - Cada vela terminada empuja su rango dentro del promedio móvil seleccionado para producir el umbral adaptativo.
   - El umbral es siempre al menos un paso de precio para evitar sobres de distancia cero.
3. **Mantenimiento de columpios**
   - En cada vela, el algoritmo actualiza el máximo y el mínimo almacenados cada vez que se imprime un nuevo máximo o mínimo absoluto.
   - Cuando la tendencia cambia, el extremo relevante se registra como un pivote y, si el gráfico está habilitado, se conecta con el pivote opuesto mediante una línea.
4. **Reglas de ruptura**
   - Régimen alcista (`_isTrendUp == true`): un cierre por debajo de `_highestPrice - threshold` desencadena una reversión a corto. El tamaño de la orden es igual a `Volume + |Position|`, por lo que la exposición existente se reduce y se abre una nueva posición corta en una sola llamada.
   - Régimen bajista (`_isTrendUp == false`): un cierre por encima de `_lowestPrice + threshold` refleja la lógica y se invierte en largo.
5. **Detener gestión**
   - Cuando `StopLossMultiplier` es mayor que cero, el precio de entrada se compensa con `threshold * StopLossMultiplier` (limitado a al menos tres pasos de precio) para derivar un nivel de parada sintético.
   - Si una vela atraviesa el stop largo con su mínimo o el stop corto con su máximo, la posición se aplana mediante una orden de mercado.
6. **Infraestructura**
   - `StartProtection()` habilita los mecanismos de seguridad integrados StockSharp tan pronto como se lanza la estrategia.
   - Todas las acciones son impulsadas por velas terminadas para emular el ciclo de recálculo barra por barra del asesor experto original.

## Diferencias con la versión MQL
- El experto MetaTrader recalcula en cada tick, mientras que este puerto opera con velas completadas porque las suscripciones a velas son la fuente de datos idiomáticos en el nivel alto API.
- Las restricciones específicas del corredor, como `MODE_STOPLEVEL`, no están disponibles; en cambio, la compensación de parada está limitada por tres pasos de precio para proporcionar un retroceso conservador.
- Las órdenes se revierten combinando las cantidades de cierre y apertura en una única llamada `BuyMarket`/`SellMarket` en lugar de iterar sobre las posiciones existentes.
- La visualización se basa en StockSharp primitivas del gráfico (`DrawLine`) en lugar de objetos de plataforma, pero la disposición de las líneas de pivote a pivote coincide con la salida del indicador original.

## Notas de uso
- Asegúrese de que la seguridad seleccionada exponga un `PriceStep` válido. Cuando falta, el código predeterminado es `1`, que puede necesitar ajustes para ciertos instrumentos.
- Debido a que la estrategia depende de velas completadas, los plazos extremadamente pequeños reducen la confiabilidad de la estimación de volatilidad. Considere alinear `CandleType` con el período de tiempo utilizado originalmente por EA (H4 de forma predeterminada).
- Las paradas son opcionales. Dejar `StopLossMultiplier` en cero replica la gestión de riesgos sin límites del script MQL.
- El algoritmo sigue tendencias por diseño y no impone objetivos de obtención de beneficios; las salidas se producen sólo mediante la reversión del régimen o la activación de un stop-loss.
