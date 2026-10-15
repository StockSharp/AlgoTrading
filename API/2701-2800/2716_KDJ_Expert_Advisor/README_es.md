# Estrategia del Asesor Experto KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el MetaTrader 5 «KDJ Expert Advisor» de senlin ge. Opera en un solo símbolo usando señales del oscilador KDJ, una evolución del oscilador estocástico donde la línea %K se suaviza dos veces. La estrategia observa la diferencia entre las líneas %K y %D (a menudo llamada línea J) para identificar reversiones de impulso, abriendo solo una posición a la vez. La gestión de operaciones refleja el asesor experto original: cada operación recibe inmediatamente un stop-loss fijo y take-profit expresados en pips y traducidos a distancia de precio usando la configuración del instrumento.

La implementación usa la API de alto nivel de StockSharp con una suscripción de velas y el indicador `Stochastic` integrado, configurado para coincidir con los parámetros KDJ de la versión MQL5. El código detecta automáticamente símbolos Forex de 3 o 5 dígitos y ajusta el valor del pip en consecuencia.

## Lógica del indicador
El indicador subyacente funciona en tres etapas:

1. **Cálculo RSV** – Para cada vela terminada, calcular el Valor Estocástico Bruto en `KDJ Length` velas:
   \[
   RSV = \frac{Close - LowestLow}{HighestHigh - LowestLow} \times 100
   \]
2. **Suavizado %K** – Promediar los últimos valores `Smooth %K` de RSV para obtener la línea %K.
3. **Suavizado %D** – Promediar los últimos valores `Smooth %D` de %K para obtener la línea %D.

La estrategia luego analiza `K - D` (referido como *KDC* en la fuente original) y la pendiente de %K para detectar reversiones.

## Criterios de entrada
Una posición de mercado se abre solo si no hay ninguna posición existente para el símbolo. Las señales se evalúan en velas completadas:

- **Compra** cuando cualquiera de las siguientes condiciones es verdadera:
  - `K - D` cruza por encima de cero (de negativo a positivo); o
  - `K - D` está por encima de cero y la línea %K está subiendo (`K_current > K_previous`).
- **Venta** cuando cualquiera de las siguientes condiciones es verdadera:
  - `K - D` cruza por debajo de cero (de positivo a negativo); o
  - `K - D` está por debajo de cero y la línea %K está bajando (`K_current < K_previous`).

Esto coincide con la estructura booleana del asesor experto MQL5 original, garantizando un timing de operación idéntico.

## Gestión de riesgos
- Cada orden ejecutada recibe un stop-loss protector y take-profit, medidos en pips y convertidos a distancia de precio a través del tamaño del tick del instrumento. Un valor de cero deshabilita el tramo de protección correspondiente.
- La estrategia no realiza pirámide ni promedia posiciones. Permanece plana hasta que la posición actual sea cerrada por las órdenes protectoras o por intervención manual.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| **Candle Type** | Tipo de datos/marco temporal de las velas de entrada. | Marco temporal de 15 minutos |
| **KDJ Length** | Número de velas para el cálculo RSV. | 30 |
| **Smooth %K** | Número de valores RSV usados para suavizar la línea %K. | 3 |
| **Smooth %D** | Número de valores %K usados para suavizar la línea %D. | 6 |
| **Stop Loss (pips)** | Distancia del stop-loss protector. Establezca en 0 para deshabilitar. | 25 |
| **Take Profit (pips)** | Distancia del take-profit protector. Establezca en 0 para deshabilitar. | 45 |
| **Order Volume** | Cantidad enviada con las órdenes de mercado. | 1 |

Todos los parámetros admiten rangos de optimización idénticos a las entradas del asesor experto original.

## Notas de uso
1. Configure el instrumento y el conector deseados en el tester o en el entorno en vivo.
2. Ajuste el tipo de vela para coincidir con el marco temporal del gráfico que desea emular desde MetaTrader.
3. Opcionalmente optimice los parámetros KDJ, stop-loss, take-profit o volumen de la orden.
4. Inicie la estrategia. Las órdenes se generan solo en velas completamente formadas.
5. El gráfico muestra automáticamente velas, el indicador KDJ y las operaciones ejecutadas para confirmación visual.

## Diferencias con el EA original
- Usa el indicador `Stochastic` de StockSharp con períodos de suavizado para replicar los buffers KDJ de MQL5; no se requiere ningún archivo de indicador externo.
- Las órdenes protectoras se gestionan a través de `StartProtection`, que envía salidas de mercado cuando se activan.
- El volumen es un parámetro fijo en lugar del modelo de riesgo `MoneyFixedMargin` de MQL5, manteniendo la implementación concisa y enfocada en la lógica de señal.
