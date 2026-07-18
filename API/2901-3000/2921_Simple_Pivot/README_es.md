# Estrategia de Pivote Simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto "SimplePivot" de MetaTrader 5. Evalúa continuamente la relación entre la apertura de la barra actual y el nivel de pivote de la barra anterior, manteniendo siempre una única posición direccional. Cuando el sesgo cambia, la estrategia cierra la posición existente y abre inmediatamente una en la dirección opuesta.

## Descripción general

- **Régimen de mercado**: Trading de swing siempre en el mercado.
- **Instrumentos**: Cualquier instrumento que proporcione datos de velas para el período de tiempo seleccionado.
- **Períodos de tiempo**: Configurable mediante el parámetro *Candle Type* (velas de 1 hora por defecto).
- **Órdenes**: Órdenes de mercado dimensionadas por el parámetro *Volume*.

## Cómo funciona

### Cálculo del pivote

1. Esperar al menos una vela completada para inicializar el cálculo.
2. Calcular el pivote de la vela anterior como la media aritmética de sus precios máximo y mínimo.
3. Retener el máximo y mínimo anteriores para que el pivote de la siguiente barra pueda producirse inmediatamente cuando termine una nueva vela.

### Decisión direccional

1. El sesgo predeterminado es largo (compra).
2. Si la vela actual abre por debajo del máximo anterior mientras permanece por encima del pivote, el sesgo cambia a corto (venta).
3. Si la dirección deseada no cambia respecto a la última operación ejecutada, se preserva la posición existente y no se envían nuevas órdenes.

### Gestión de posición

1. Si la dirección deseada difiere de la operación actual, la posición en ejecución se liquida mediante una orden de mercado opuesta.
2. Después de liquidar, una orden de mercado dimensionada por *Volume* establece la nueva exposición direccional.
3. El proceso se repite en cada vela completada, asegurando que la estrategia esté siempre larga o corta.

## Parámetros

- **Volume**: Tamaño de la operación usado para cada entrada. También determina el tamaño de la orden de cierre cuando la estrategia cambia de dirección.
- **Candle Type**: Tipo de datos de las velas usadas para los cálculos de pivote y entrada. El valor predeterminado es un período de 1 hora, pero se puede seleccionar cualquier período disponible.

## Notas adicionales

- La lógica reacciona en velas completamente cerradas (`CandleStates.Finished`) para evitar señales repetidas mientras una vela todavía se está formando.
- No se definen stops ni objetivos de beneficio; las salidas ocurren solo cuando la regla de pivote solicita un cambio de dirección.
- Debido a que la estrategia siempre está en el mercado, los controles de riesgo como el monitoreo de máxima drawdown o los filtros de sesión deben manejarse externamente si se requieren.
