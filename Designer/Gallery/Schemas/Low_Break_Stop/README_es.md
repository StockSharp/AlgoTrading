# Ejemplo de Estrategia de Ruptura de Mínimos con Stop en StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

Este ejemplo demuestra una estrategia de trading de "Ruptura de Mínimos con Stop" configurada en StockSharp Strategy Designer. Está diseñada para ejecutar operaciones basadas en condiciones específicas de ruptura del precio mínimo, incorporando parámetros de stop-loss para gestionar el riesgo. Esta estrategia aprovecha los datos de mercado en tiempo real para identificar cuándo el precio de un valor rompe por debajo de un mínimo predefinido durante un período determinado y luego inicia operaciones con condiciones de stop definidas.

![schema](schema.png)

## Descripción del Esquema

El esquema proporcionado en el archivo JSON describe un flujo de trabajo detallado para operar en función de la acción del precio en relación con los mínimos históricos:

1. **Nodo de Instrumento**: es el nodo de entrada principal donde se [define el instrumento objetivo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html), sirviendo como base para la entrada de datos relacionados con los precios de mercado.

2. **Nodo TimeFrameCandle**: procesa los datos de mercado entrantes para generar [velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), que son fundamentales para analizar los movimientos de precios en intervalos de tiempo específicos.

3. **Nodos de Indicador de Mínimos**: estos nodos [calculan el precio más bajo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) durante un número determinado de períodos, identificando niveles potenciales de ruptura para iniciar operaciones.

4. **Nodos de Comparación**: se utilizan para [comparar](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) el precio actual con el mínimo histórico, activando señales de trading cuando el precio cae por debajo del umbral establecido, indicando una ruptura bajista.

5. **Nodo del Panel de Gráficos**: visualiza los datos de trading e indicadores, proporcionando una [representación gráfica](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) de las operaciones de la estrategia, esencial para el monitoreo en tiempo real y los ajustes de la estrategia.

6. **Nodos de Ejecución de Operaciones (Compra/Venta)**: son responsables de [ejecutar las operaciones](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) según la lógica de la estrategia. En este caso, se puede ejecutar una orden de venta para aprovechar el movimiento esperado a la baja.

7. **Nodo de Orden Stop**: implementa condiciones de [stop-loss](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) para gestionar el riesgo de manera efectiva. Esto garantiza que las operaciones se cierren en un umbral de pérdida predefinido para protegerse contra movimientos adversos significativos.

## Flujo de Trabajo

- El **Nodo de Instrumento** suministra los datos de mercado necesarios para la estrategia.
- Estos datos fluyen al **Nodo TimeFrameCandle**, donde se transforman en formatos de velas utilizables.
- Los **Nodos de Indicador de Mínimos** analizan estas velas para determinar los mínimos históricos.
- Los **Nodos de Comparación** monitorean el precio actual del mercado en comparación con estos mínimos, activando operaciones cuando el precio cae por debajo del mínimo histórico.
- Los **Nodos de Ejecución de Operaciones** utilizan estas señales para ejecutar órdenes de venta asumiendo una continuación de la tendencia bajista.
- Simultáneamente, los **Nodos de Orden Stop** establecen órdenes de stop-loss basadas en criterios predefinidos para gestionar pérdidas potenciales.
- El **Nodo del Panel de Gráficos** muestra todas las transacciones y movimientos de precios, proporcionando retroalimentación visual sobre el rendimiento de la estrategia.

## Aplicación Práctica

Esta configuración es especialmente útil para traders que se centran en estrategias de ruptura, donde reconocer y actuar sobre movimientos de precios significativos puede llevar a oportunidades rentables. La estrategia es adecuada para:
- mercados de alta volatilidad donde las oscilaciones de precios pueden proporcionar importantes oportunidades de trading;
- traders intradía que capitalizan movimientos rápidos de precios y necesitan mecanismos robustos para gestionar riesgos eficazmente.

## Conclusión

El ejemplo de la estrategia "Ruptura de Mínimos con Stop" dentro de StockSharp Strategy Designer muestra un enfoque avanzado del trading algorítmico al combinar el procesamiento de datos en tiempo real con sofisticadas técnicas de gestión de riesgos. Esta estrategia proporciona un marco dinámico para explotar las rupturas de precios garantizando al mismo tiempo que los parámetros de riesgo se respeten estrictamente, lo que la convierte en una herramienta esencial para los traders que buscan maximizar sus rendimientos a través de métodos de trading precisos y controlados.
