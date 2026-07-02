# Descripción de la Estrategia Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General de la Estrategia

La estrategia "Parabolic SAR" está diseñada para capturar reversiones e continuaciones de tendencia utilizando el indicador Parabolic Stop and Reverse (SAR) dentro de [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html). Esta estrategia proporciona señales claras de entrada y salida basadas en el movimiento del precio en relación con los puntos del Parabolic SAR.

![schema](schema.png)

## Detalles de la Estrategia

### Componentes

- **Formación de Velas**: utiliza un marco de tiempo de 5 minutos para [analizar la acción del precio](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html), garantizando que la estrategia capture los movimientos de mercado a corto plazo de forma efectiva.
- **Indicador Parabolic SAR**: [configurado](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) con un factor de aceleración inicial de 0,02, un paso de aceleración de 0,02 y una aceleración máxima de 0,2. Estos ajustes permiten que el indicador se adapte a la volatilidad del mercado.

### Ejecución de Operaciones

- **Señal de Entrada**: se genera una señal de compra cuando el precio cruza [por encima](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) de los puntos del Parabolic SAR, indicando una posible tendencia alcista.
- **Señal de Salida**: se emite una señal de venta cuando el precio cae [por debajo](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) de los puntos del Parabolic SAR, sugiriendo una posible tendencia bajista.

### Visualización

- **Visualización en Gráfico**: los puntos del Parabolic SAR se trazan en el [gráfico](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) junto con las velas de precios, proporcionando una representación visual de la tendencia y las posibles señales de trading.

## Detalles de Implementación

- **Plataforma**: implementada en la plataforma StockSharp aprovechando sus completas funciones de obtención de datos en tiempo real, cálculo de indicadores y ejecución de operaciones.
- **Aplicación del Indicador**: el Parabolic SAR se aplica directamente al gráfico de precios, lo que permite una evaluación visual inmediata de los cambios de tendencia y la validez de las configuraciones de trading.

## Conclusión

La estrategia "Parabolic SAR" es ideal para traders que necesitan señales de trading precisas y automáticas basadas en patrones de reversión de tendencia. Aprovecha la naturaleza dinámica del Parabolic SAR para proporcionar entradas y salidas oportunas, aumentando el potencial de ganancias en mercados de rápido movimiento.
