# Estrategia de red MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una adaptación de C# del asesor experto MetaTrader 5 **MAGrid.mq5**. Mantiene una cuadrícula cubierta de posiciones de compra y venta en torno a una media móvil exponencial (EMA). La idea es mantener la cuadrícula equilibrada alrededor del ancla EMA. Cuando el precio cruza pasos de distancia predefinidos por encima o por debajo del EMA, la estrategia cierra una posición desde el lado opuesto de la cuadrícula y abre una nueva posición en la dirección de la ruptura. Esto vuelve a centrar constantemente la cesta alrededor de la media móvil.

## Fuente original

- **MQL carpeta del repositorio:** `MQL/38303`
- **Archivo original:** `MAGrid.mq5`
- **Plataforma:** MetaTrader 5 (modo de cobertura)

## Lógica de trading

1. **EMA Ancla**
   - El período EMA es configurable (predeterminado 48).
   - El EMA se calcula sobre la serie de velas seleccionada.
   - Los niveles de cuadrícula se calculan como múltiplos del parámetro `Distance` encima y debajo de EMA.

2. **Inicialización de cuadrícula**
   - El tamaño efectivo de la cuadrícula debe ser uniforme para reflejar ambos lados alrededor del EMA.
   - El índice de cuadrícula actual se determina comparando el último precio de cierre con los niveles basados en EMA.
   - Se abre una cesta simétrica de órdenes de mercado de compra y venta de modo que la mitad de las posiciones se sitúan por debajo del EMA y la otra mitad por encima.

3. **Mantenimiento de red**
   - Cuando el precio cierra por encima del siguiente nivel superior de la red, la estrategia:
     - Incrementa el índice de la cuadrícula.
     - Cierra una orden larga si queda alguna exposición.
     - Abre una nueva orden corta para extender la mitad superior de la cuadrícula.
   - Cuando el precio cierra por debajo del siguiente nivel inferior de la red, la estrategia:
     - Disminuye el índice de la cuadrícula.
     - Cierra una orden corta si queda alguna exposición.
     - Abre una nueva orden larga para reconstruir la mitad inferior de la cuadrícula.
   - Si un lado de la cuadrícula se queda sin exposición, el activador correspondiente se desactiva hasta que se abran nuevas órdenes.

4. **Manejo de pedidos**
   - Los pedidos se rastrean a través de un mapa interno simple para distinguir entre despachos de apertura y cierre.
   - La estrategia almacena contadores de exposición separados para las cestas largas y cortas. Esto refleja el comportamiento de cobertura de la versión MQL mientras se utiliza el modelo de posición neta de StockSharp.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `MaPeriod` | 48 | EMA período utilizado para el nivel de anclaje. |
| `GridAmount` | 6 | Número de pasos de la cuadrícula; redondeado automáticamente hacia arriba a un valor par. |
| `Distance` | 0.005 | Espaciado relativo entre niveles de cuadrícula (por ejemplo, 0,005 = 0,5%). |
| `OrderVolume` | 0.1 | Volumen presentado con cada orden de mercado. |
| `CandleType` | Marco de tiempo diario | Serie de velas utilizada para calcular el EMA y evaluar señales. |

## Gestión del riesgo

- La estrategia no implementa reglas de stop-loss o take-profit; El riesgo se controla mediante el número de pasos de la cuadrícula y el volumen del pedido.
- Debido a que la red mantiene una exposición tanto larga como corta, el valor de la cartera puede permanecer relativamente estable, pero el uso del margen crece con el tamaño y la distancia de la red.
- Considere el uso de controles de riesgo de cartera (reducción máxima, uso de capital) a nivel de estrategia o cartera.

## Notas de conversión

- La implementación de C# reproduce la lógica de cobertura mediante el seguimiento por separado de la exposición larga y corta.
- El cálculo del volumen dependiente de la cuenta de MQL se ha reemplazado con un parámetro configurable `OrderVolume` para mayor claridad.
- Las suscripciones a Candle dependen dla API de alto nivel de StockSharp usando `SubscribeCandles().Bind(...)` de acuerdo con las pautas del proyecto.
