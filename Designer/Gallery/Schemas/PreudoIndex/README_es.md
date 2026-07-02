# Descripción de la Estrategia PseudoIndex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General de la Estrategia

La estrategia "PseudoIndex" está diseñada para crear un índice sintético a partir de los ratios de precios de dos criptomonedas principales, concretamente Ethereum y Bitcoin, tal como se negocian en el exchange Binance. Esta estrategia monitorea el rendimiento relativo de estas criptomonedas calculando un índice en tiempo real basado en sus movimientos de precios.

![schema](schema.png)

## Detalles de la Estrategia

### Componentes

- **Fuentes de Datos**: utiliza datos de [precio en tiempo real](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) de ETHUSDT y BTCUSDT de Binance.
- **Cálculo de Precio**:
  - Rastrea los [precios de cierre](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) tanto de ETHUSDT como de BTCUSDT.
  - Calcula el ratio de estos precios para formar un índice sintético que representa el rendimiento relativo de Ethereum frente a Bitcoin.

### Cálculo del Índice

- **Formación de Velas**: utiliza un [marco de tiempo de 5 minutos](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) tanto para ETH como para BTC, con el fin de capturar movimientos de precios a corto plazo.
- **Cálculo del Ratio**: el índice se calcula como el precio de ETH [dividido](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/formula.html) por el precio de BTC, proporcionando una medida de cómo evoluciona el valor de Ethereum en relación con Bitcoin.

### Visualización

- **Visualización en Gráfico**: el índice resultante se representa en un [gráfico](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) para análisis visual, ayudando a identificar tendencias y posibles señales de trading basadas en el movimiento del índice.

## Detalles de Implementación

- **Plataforma**: implementada dentro de la plataforma StockSharp utilizando sus funciones avanzadas para la obtención y procesamiento de datos en tiempo real.
- **Indicadores Técnicos**: la estrategia se apoya en información básica de precios sin el uso de indicadores técnicos adicionales, centrándose en el ratio de precios para la toma de decisiones.

## Conclusión

La estrategia "PseudoIndex" ofrece un enfoque novedoso para el trading al comparar el rendimiento de dos criptomonedas principales, permitiendo a los traders evaluar el sentimiento del mercado y tomar decisiones informadas basadas en la fortaleza relativa de Ethereum y Bitcoin. Esto puede ser especialmente útil para traders que buscan cubrir o diversificar sus tenencias de criptomonedas basándose en estos análisis.
