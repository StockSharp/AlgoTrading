# Descripción de StDevStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General de la Estrategia

La "StDevStrategy" está diseñada para [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) con el fin de aprovechar los patrones de volatilidad estadística utilizando el indicador Standard Deviation. Esta estrategia está construida para identificar posibles oportunidades de trading basándose en las desviaciones del precio promedio, señalando condiciones de sobrecompra o sobreventa.

![schema](schema.png)

## Detalles de la Estrategia

### Componentes

- **Indicadores Standard Deviation**: utiliza múltiples longitudes para capturar la volatilidad a corto y largo plazo.
  - **Std Dev 20**: mide la volatilidad durante [20 períodos](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html).
  - **Lowest 15 y Highest 15**: rastrean los valores mínimos y máximos durante 15 períodos para detectar condiciones de ruptura.
  - **Lowest 50**: captura mínimos de precios a largo plazo para evaluar condiciones de mercado extendidas.

### Ejecución de Operaciones

- **Tipo de Orden**: ejecuta operaciones usando [órdenes de mercado](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) para garantizar una respuesta rápida a los cambios de señal.
- **Entrada y Salida**:
  - **Compra**: se activa cuando la acción del precio sugiere un rebote desde condiciones de sobreventa.
  - **Venta**: se inicia cuando la acción del precio indica una posible caída desde condiciones de sobrecompra.
- **Gestión de Posición**: emplea una estrategia de dimensionamiento de posición dinámico que se ajusta según la volatilidad del mercado y los parámetros de riesgo.

### Gestión de Riesgos

- **Stop Loss y Take Profit**:
  - Se establece [stop loss](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) al 1% por debajo de la entrada para minimizar el riesgo.
  - El [take profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) se fija en el 2%, capturando posibles subidas mientras se protegen las ganancias.

## Detalles de Implementación

- **Plataforma**: implementada dentro de la plataforma StockSharp aprovechando sus herramientas integrales para el análisis de datos en tiempo real y la gestión de órdenes.
- **Indicadores Técnicos**: integra múltiples instancias de Standard Deviation junto con el seguimiento de precios máximos y mínimos para mejorar la precisión del trading.

## Conclusión

La "StDevStrategy" está diseñada para traders que prefieren el análisis técnico y se centran en capturar movimientos de precios impulsados por la volatilidad. Proporciona un enfoque estructurado del trading mediante el uso de indicadores avanzados para gestionar de manera efectiva los puntos de entrada y salida.
