# Ejemplo de Manejo de Fecha y Hora en StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

Este ejemplo en StockSharp Strategy Designer demuestra una configuración sofisticada que integra el manejo de fecha y hora dentro de una estrategia de trading. La estrategia utiliza condiciones específicas de tiempo para tomar decisiones de trading basadas en los datos de velas y la hora del día, lo que lo convierte en un ejemplo práctico para escenarios donde las operaciones son sensibles al tiempo.

![schema](schema.png)

## Descripción del Esquema

El esquema presentado en el archivo JSON describe una interacción compleja entre varios nodos que manejan datos basados en tiempo para activar acciones de trading:

1. **Nodo TimeFrameCandle**: procesa los [datos de velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) para un marco de tiempo especificado. Es fundamental para las estrategias que se basan en movimientos históricos de precios para predecir tendencias futuras.

2. **Nodos OpenTime y CloseTime**: [extraen](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) los tiempos de apertura y cierre de los datos de velas, que son críticos para determinar los períodos específicos durante los cuales se evalúan las condiciones de trading.

3. **Nodos de Comparación (Equals, Greater Than)**: [comparan](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) tiempos específicos (como 14:00:00 o 15:00:00) con el tiempo actual extraído de los datos de velas. Esta configuración permite a la estrategia activarse o desactivarse según si coincide con los tiempos especificados.

4. **Nodo del Panel de Gráfico**: implementa [componentes de visualización](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) que probablemente muestran datos de trading e indicadores en un formato comprensible, ayudando en la toma de decisiones en tiempo real y en los ajustes de la estrategia.

5. **Nodos de Trading (Compra, Venta)**: se activan cuando se cumplen ciertas condiciones de tiempo, permitiendo a la estrategia ejecutar [órdenes de compra o venta](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) basándose en los resultados de comparación y la lógica de trading definida dentro de la estrategia.

## Flujo de Trabajo

- El **Nodo TimeFrameCandle** recopila y procesa datos de velas a intervalos regulares.
- Los **Nodos OpenTime y CloseTime** analizan estos datos para extraer puntos de tiempo específicos.
- Los **Nodos de Comparación** verifican estos tiempos contra valores predefinidos (por ejemplo, 14:00:00 para una condición de entrada y 15:00:00 para una condición de salida).
- Cuando se cumplen las condiciones (por ejemplo, la hora actual es igual a 14:00:00), los nodos de trading (Compra o Venta) se activan para ejecutar operaciones según la lógica de la estrategia.
- El **Nodo del Panel de Gráfico** representa visualmente estas operaciones y los datos de velas, proporcionando una visión clara del funcionamiento de la estrategia y las condiciones del mercado.

## Aplicación Práctica

Esta configuración es especialmente útil para estrategias que necesitan ejecutar operaciones en momentos específicos del día, tales como:
- **Rupturas del Rango de Apertura**, donde las operaciones se colocan alrededor de la apertura de una sesión de mercado.
- **Estrategias de Subasta de Cierre**, orientadas a los movimientos de precios y las variaciones de liquidez que ocurren al cierre de la sesión de trading.

## Conclusión

Este ejemplo de StockSharp Strategy Designer ilustra un marco sólido para desarrollar estrategias de trading sensibles al tiempo que pueden ejecutar operaciones automáticamente en momentos predefinidos. Es una excelente demostración de cómo los traders pueden aprovechar las capacidades del Strategy Designer para crear estrategias de trading complejas, basadas en reglas, que responden dinámicamente a los datos del mercado en tiempo real y a condiciones temporales específicas.
