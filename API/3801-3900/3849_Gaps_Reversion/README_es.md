# Estrategia de reversión de brechas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de reversión de brechas** es una adaptación directa del MetaTrader4 asesor experto `gaps.mq4`. El sistema monitorea velas de 15 minutos y baño.
ks para abrir brechas que ocurren fuera del rango máximo/bajo de la vela anterior. Cuando aparece tal brecha, la estrategia inmediatamente e
entra en el mercado en la dirección del movimiento de reversión a la media esperado.

La versión StockSharp sigue la lógica original y depende de la suscripción de vela de alto nivel API. Toda la gestión comercial es
se realiza con órdenes de mercado y no se colocan órdenes de protección fijas, lo que refleja el comportamiento encontrado en el código MQL.

## Reglas de trading

1. Suscríbete a velas de 15 minutos (configurable a través del parámetro `CandleType`).
2. Mantenga el máximo y el mínimo de la vela completada anteriormente.
3. Cuando comienza una nueva vela:
   - Calcule el buffer de brecha: `(MinGapSize + spreadInSteps) * pointValue`.
   - Si el precio de apertura está **por encima** de `previousHigh + gapBuffer`, abra una posición **corta**.
   - Si el precio de apertura está **por debajo** de `previousLow - gapBuffer`, abra una posición **larga**.
4. Sólo se permite una operación por vela. Una vez que se realiza una orden, la estrategia espera la siguiente vela antes de generar una nueva vela.
señal.

El componente de diferencial utiliza la mejor oferta/demanda actual, si está disponible. Cuando no se proporcionan datos de cotización, la estrategia vuelve a caer en pecado.
Un pequeño paso en el precio como amortiguador conservador.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `MinGapSize` | `1` | Tamaño mínimo de brecha en los escalones de precios que se debe exceder antes de enviar un pedido. |
| `GapVolume` | `0.1` | Volumen de pedidos para entradas al mercado provocadas por brechas. |
| `CandleType` | `15m TimeFrame` | Tipo de vela utilizado para los cálculos (el valor predeterminado es velas de 15 minutos). |

Todos los parámetros están registrados como `StrategyParam<T>` y admiten la optimización dentro de StockSharp Designer u otras herramientas.

## Notas de implementación

- Utiliza `SubscribeCandles` con `Bind` para procesar velas terminadas únicamente.
- Recuerda el rango de la vela anterior para evitar volver a calcular la serie de datos.
- Bloquea órdenes duplicadas en la misma vela rastreando el tiempo de apertura de la barra que desencadenó la operación.
- La salida del gráfico dibuja las velas suscritas y las operaciones de estrategia para una inspección visual rápida.

## Diferencias con la versión MQL

- Los niveles de toma de ganancias y límite de pérdidas no se establecieron correctamente en el EA original (el código MQL pasó valores a los parámetros incorrectos)
. Por tanto, el puerto StockSharp mantiene el comportamiento de funcionamiento sin órdenes de protección.
- El manejo de diferenciales ahora verifica las cotizaciones de oferta y demanda en tiempo real cuando están disponibles, lo que proporciona un margen más adaptable.

## Requisitos

- StockSharp API con acceso a los datos de velas para el instrumento seleccionado.
- Las cotizaciones de nivel 1 son opcionales pero mejoran la detección de diferenciales.
