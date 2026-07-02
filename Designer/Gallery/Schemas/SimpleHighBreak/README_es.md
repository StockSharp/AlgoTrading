# Descripción de la Estrategia SimpleHighBreak
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General de la Estrategia

La estrategia "SimpleHighBreak" está diseñada para aprovechar las rupturas de precio por encima de un máximo predefinido dentro de [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html). Esta estrategia se centra en identificar oportunidades donde el precio rompe por encima del máximo de los últimos 15 períodos, señalando una posible continuación de la tendencia alcista.

![schema](schema.png)

## Detalles de la Estrategia

### Componentes

- **Formación de Velas**: utiliza un marco de tiempo de 5 minutos para generar [velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), monitoreando el mercado en busca de movimientos de precios significativos.
- **Indicador de Máximo**: calcula el [precio más alto](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) durante los últimos 15 períodos para establecer niveles de ruptura.
- **Detección de Ruptura**: la estrategia activa una orden de compra cuando el precio actual rompe [por encima](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) del máximo reciente de 15 períodos.

### Ejecución de Operaciones

- **Tipo de Orden**: [Orden](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) de mercado.
- **Entrada**: se coloca una orden de compra cuando el precio supera el máximo de 15 períodos.
- **Estrategia de Salida**: la posición se cierra en función de condiciones específicas, como un marco de tiempo establecido o un patrón de reversión, gestionados dinámicamente por la estrategia.

### Gestión de Riesgos

- **Tamaño de Posición**: adapta el tamaño de la posición según reglas predefinidas de gestión de riesgos y la volatilidad actual del mercado.
- **Stop Loss y Take Profit**: los niveles configurables de [stop loss y take profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) se establecen inmediatamente después de la entrada para gestionar el riesgo y asegurar ganancias.

## Detalles de Implementación

- **Plataforma**: implementada dentro de la plataforma StockSharp utilizando sus extensas funciones para el procesamiento de datos en tiempo real y la gestión automatizada de órdenes.
- **Indicadores**: utiliza principalmente el indicador de precio máximo durante un número específico de períodos para determinar los puntos de entrada.

## Conclusión

La estrategia "SimpleHighBreak" ofrece un enfoque sencillo pero efectivo para el trading de rupturas de precio, ideal para traders que buscan oportunidades en mercados volátiles. Combina indicadores técnicos con una gestión detallada del riesgo para maximizar los rendimientos potenciales al tiempo que minimiza los riesgos.
