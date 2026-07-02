# Descripción de la Estrategia de Ruptura de Mínimos con Cálculo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General de la Estrategia

La estrategia "Ruptura de Mínimos con Cálculo" utiliza una combinación de indicadores de precio máximo y mínimo para identificar posibles puntos de ruptura en el mercado. Esta estrategia tiene como objetivo ejecutar operaciones cuando el precio rompe por debajo de un mínimo calculado durante un período específico, lo que sugiere una posible tendencia bajista.

[![schema](schema.png)](schema_easter_egg.png)

## Detalles de la Estrategia

### Componentes

- **Formación de Velas**: utiliza un marco de tiempo de una hora para la generación de [velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), capturando movimientos de mercado significativos.
- **Indicadores de Máximos y Mínimos**:
  - **Highest 25**: rastrea el [precio más alto](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) durante los últimos 25 períodos.
  - **Lowest 45**: monitorea el [precio más bajo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) durante los últimos 45 períodos.
- **Lógica de Cálculo**: determina los puntos de ejecución de operaciones [comparando](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) los precios actuales con los niveles de máximo y mínimo calculados por los indicadores.

### Ejecución de Operaciones

- **Señal de Entrada**: se inicia una orden de [compra](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) cuando el precio actual cruza [por debajo]() del punto mínimo calculado por el indicador "Lowest 45".
- **Señal de Salida**: se activa una orden de [venta](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) cuando la acción del precio posterior no respalda la continuación de la tendencia bajista, definida por parámetros de cálculo específicos.

### Visualización

- **Visualización en Gráfico**: los valores de los indicadores "Highest 25" y "Lowest 45" se representan en el [gráfico](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) junto con las velas de precios, proporcionando una representación visual de los posibles puntos de ruptura.

## Detalles de Implementación

- **Plataforma**: implementada en la plataforma StockSharp, aprovechando sus capacidades para el procesamiento de datos en tiempo real y el cálculo de indicadores.
- **Uso de Indicadores**: emplea indicadores de máximo y mínimo para establecer un rango dentro del cual la estrategia busca puntos de ruptura.

## Conclusión

La estrategia "Ruptura de Mínimos con Cálculo" está diseñada para traders que buscan oportunidades basadas en rupturas de precios desde máximos o mínimos establecidos. Esta estrategia combina indicadores técnicos con una lógica de cálculo sofisticada para identificar y actuar sobre posibles movimientos del mercado.
