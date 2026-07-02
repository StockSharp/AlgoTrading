# Descripción de la Estrategia 3 Black Crows Trend en StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general de la estrategia

La estrategia "3 Black Crows Trend" en el [Strategy Designer](https://doc.stocksharp.com/topics/designer.html) emplea un patrón específico de velas de reversión bajista para predecir posibles movimientos a la baja en el mercado bursátil. Este esquema de trading automatizado está meticulosamente diseñado para reconocer y operar en función de patrones de precio significativos, con el objetivo de beneficiarse de las tendencias bajistas.

![schema](schema.png)

## Detalles de la estrategia

### Detección de patrón: 3 Black Crows

- **Descripción**: Este módulo identifica el [patrón](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html) "3 Black Crows", que señala una posible reversión bajista tras una tendencia alcista. El patrón consiste en tres velas consecutivas de cuerpo largo que cierran por debajo de sus precios de apertura, con la apertura de cada sesión ocurriendo dentro del cuerpo de la vela anterior.
- **Condiciones**:
  - Vela 1: Open > Close
  - Vela 2: Open > Close y Open < Previous Open
  - Vela 3: Open > Close y Open < Previous Open

### Ejecución de operaciones

- **Tipo de orden**: [Orden](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) de mercado
- **Entrada**: Inicia una orden de venta al confirmar el patrón "3 Black Crows".
- **Estrategia de salida**:
  - **Take Profit**: Establecido al 3% por encima del precio de entrada.
  - **Stop Loss**: Establecido al 1% por debajo del precio de entrada.
- **Gestión del riesgo**: La estrategia se adhiere estrictamente a la configuración inicial de [stop loss y take profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) sin seguimiento.

### Condiciones de trading

- **Frecuencia**: Opera en un [marco temporal diario](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), procesando nuevas formaciones de velas al final de cada jornada de trading.
- **Orden de mercado**: Garantiza una ejecución rápida [colocando operaciones](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) a los precios de mercado vigentes.

## Detalles de implementación

- **Plataforma**: Implementada en la plataforma StockSharp, que ofrece funciones completas para la detección de patrones y la ejecución automatizada de operaciones.
- **Configuración**:
  - **Nivel de registro**: Configurable para facilitar información operativa detallada.
  - **Visualización de parámetros**: Configuración de visualización personalizable para transparencia operativa.
  - **Procesamiento de valores nulos**: Manejo configurable de valores nulos para mejorar la robustez y la fiabilidad.

## Conclusión

La estrategia "3 Black Crows Trend" está diseñada para traders que se centran en identificar y aprovechar los patrones de reversión bajista. Combina un reconocimiento preciso de patrones con reglas estrictas de ejecución de operaciones para mejorar la rentabilidad potencial en escenarios bajistas del mercado.
