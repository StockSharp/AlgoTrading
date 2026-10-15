# Ejemplo de Estrategia con Fórmulas y Expresiones Matemáticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este archivo contiene un ejemplo detallado de una estrategia de trading diseñada usando la plataforma Designer de StockSharp. La estrategia integra expresiones y fórmulas matemáticas para ejecutar operaciones basadas en condiciones específicas cumplidas por indicadores técnicos.

## Descripción de la Estrategia

Este esquema demuestra la aplicación de dos indicadores técnicos populares para tomar decisiones de trading:

### Estrategia de Bandas de Bollinger
- **Condición de Compra**: Se activa una orden de compra cuando la vela de precio cruza hacia arriba la curva superior del indicador Bollinger Bands.
- **Condición de Venta**: Se ejecuta una orden de venta cuando la vela de precio cruza hacia abajo la curva inferior del indicador Bollinger Bands.

### Estrategia del Indicador MACD
- **Condición de Compra**: Inicia una orden de compra cuando la curva MACD cambia su signo de negativo a positivo.
- **Condición de Venta**: Activa una orden de venta cuando la curva MACD cambia su signo de positivo a negativo.

## Características Adicionales

- **Comparación Visual**: El esquema permite una comparación visual lado a lado de los resultados de ambas estrategias.
- **Exportación de Resultados**: Incluye funcionalidad para exportar los resultados de las pruebas a un archivo para análisis posterior.

Este esquema proporciona un marco práctico para entender y aplicar herramientas matemáticas en estrategias de trading, aprovechando las capacidades de la plataforma Designer.
