# Ejemplo de Estrategia High Break en StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia "High Break" representada en el esquema JSON proporcionado está diseñada para ejecutar operaciones basándose en condiciones específicas relacionadas con los movimientos de precio y los marcos temporales, utilizando el StockSharp Strategy Designer. Este ejemplo muestra cómo configurar una estrategia de trading que identifica posibles oportunidades de compra cuando el precio de un valor supera un máximo predeterminado durante un período de tiempo determinado.

![schema](schema.png)

## Descripción del esquema

El esquema describe una secuencia de componentes interconectados diseñados para capturar, analizar y actuar sobre datos de mercado en tiempo real:

1. **Nodo Security**: Sirve como base, especificando el [valor](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) (por ejemplo, acciones, futuros) al que se aplica la estrategia. Este nodo es crítico ya que determina la entrada de datos para la estrategia.

2. **Nodo TimeFrameCandle**: Procesa los datos de mercado entrantes y los organiza en [velas basadas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) en un marco temporal especificado. Este nodo es vital para las estrategias que se basan en el análisis histórico de precios para tomar decisiones de trading.

3. **Nodo Highest**: Analiza los datos de velas para [determinar el precio más alto](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) alcanzado durante un período de tiempo especificado (por ejemplo, 60 minutos). Este valor establece un punto de referencia para identificar rupturas de precio significativas.

4. **Nodo de comparación**: [Compara](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) los precios actuales con el máximo histórico determinado por el nodo Highest. Si el precio actual supera este máximo, activa una posible señal de trading.

5. **Nodo Chart Panel**: [Visualiza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) los datos de precio y las acciones de la estrategia, proporcionando una representación gráfica del funcionamiento de la estrategia, lo que facilita la monitorización y los ajustes.

6. **Nodos de ejecución de operaciones (Compra/Venta)**: Responsables de [ejecutar operaciones](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) cuando se cumplen las condiciones de la estrategia. Por ejemplo, una orden de compra puede ejecutarse cuando el precio supera el máximo histórico.

## Flujo de trabajo

- El **Nodo Security** alimenta datos de mercado al **Nodo TimeFrameCandle** para crear un conjunto de datos de velas estructurado basado en el tiempo.
- El **Nodo Highest** calcula el precio más alto de estas velas durante un período definido.
- El **Nodo de comparación** compara continuamente el precio actual con este máximo. Si el precio actual supera el máximo histórico, sugiere una ruptura alcista, lo que potencialmente activa una señal de compra.
- El **Nodo Chart Panel** proporciona visualización en tiempo real, permitiendo retroalimentación visual inmediata sobre el rendimiento de la estrategia y las condiciones del mercado.
- Cuando se cumple la condición de compra, el **Nodo de ejecución de operaciones** (Compra) inicia una operación, capitalizando el impulso alcista esperado.

## Aplicación práctica

Esta configuración es especialmente útil para traders que se especializan en estrategias de ruptura, donde reconocer y actuar ante movimientos de precios por encima de ciertos umbrales puede conducir a operaciones rentables. Estas estrategias son populares en mercados volátiles donde las rupturas de precio pueden indicar tendencias fuertes.

## Conclusión

El ejemplo de la estrategia "High Break" dentro del StockSharp Strategy Designer ilustra un uso sofisticado de los datos de mercado para automatizar las decisiones de trading basándose en movimientos de precio identificados. Al aprovechar las herramientas de procesamiento de datos en tiempo real y visualización, la estrategia ayuda a los traders a capitalizar eficientemente las oportunidades de mercado presentadas por las rupturas de precio. Este ejemplo no solo demuestra el poder de la plataforma StockSharp en el desarrollo de estrategias de trading dinámicas, sino que también sirve como base para una mayor personalización y optimización basada en los requisitos individuales de trading y las condiciones del mercado.
