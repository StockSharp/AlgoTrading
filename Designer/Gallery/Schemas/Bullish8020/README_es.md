# Descripción de la Estrategia Bullish8020
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general de la estrategia

La estrategia "Bullish8020" está diseñada para [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) con el fin de capitalizar con alta precisión patrones de velas específicamente alcistas. Esta estrategia busca identificar oportunidades de mercado donde el sentimiento alcista es fuerte, utilizando un análisis de patrones único combinado con volumen y acción del precio.

![schema](schema.png)

## Detalles de la estrategia

### Detección de patrón: Bullish8020

- **Descripción**: Esta estrategia detecta un escenario alcista donde el [precio de apertura](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) está por debajo del precio de cierre y el tamaño del cuerpo es cuatro veces la suma de ambas sombras, lo que indica una fuerte presión compradora.
- **Patrón de velas**: 'Bullish8020' verifica si `(O < C) && (B >= 4*(BS+TS))`, donde `O` es apertura, `C` es cierre, `B` es tamaño del cuerpo, `BS` es sombra inferior y `TS` es sombra superior.

### Ejecución de operaciones

- **Tipo de orden**: [Orden](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) de mercado
- **Entrada**: Compra cuando el [patrón](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 'Bullish8020' se confirma, señalando un posible movimiento alcista.
- **Estrategia de salida**:
  - **Stop Loss**: Establecido al 0.5% por debajo del punto de entrada para limitar pérdidas potenciales.
  - **Condiciones de mercado**: Las operaciones se ejecutan a precios de mercado actuales para garantizar una respuesta rápida al reconocimiento del patrón.

### Gestión del riesgo

- **Dimensionamiento de posiciones**: La estrategia utiliza dimensionamiento dinámico basado en las condiciones actuales del mercado y el perfil de riesgo del trader.
- **Estrategia de Stop-Loss**: Se implementa un [stop-loss](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) estricto para protegerse contra reversiones imprevistas del mercado.

## Detalles de implementación

- **Plataforma**: Implementada en la plataforma StockSharp, aprovechando su potente API para el procesamiento de datos en tiempo real y la ejecución de órdenes.
- **Indicadores utilizados**: Combina el reconocimiento de patrones de velas con el análisis de volumen para mejorar la precisión de las señales de trading.

## Conclusión

La estrategia "Bullish8020" proporciona a los traders una herramienta robusta para explotar patrones alcistas específicos en el mercado. Está diseñada para maximizar las ganancias de configuraciones alcistas fuertes, al tiempo que emplea estrictos protocolos de gestión del riesgo para salvaguardar las inversiones.
