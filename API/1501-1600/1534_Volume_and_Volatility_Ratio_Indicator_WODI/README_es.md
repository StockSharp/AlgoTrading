# Estrategia de Indicador de Ratio de Volumen y Volatilidad WODI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simplificada derivada del script de TradingView **"Volume and Volatility Ratio Indicator - WODI"**. Monitorea el producto del volumen y la volatilidad del precio para detectar posibles reversiones. Cuando el índice de volatilidad supera un umbral dinámico y las velas recientes muestran un cambio de dirección, la estrategia abre una posición con gestión de riesgo basada en Fibonacci.

## Detalles

- **Entrada**: Volumen alto y volatilidad combinados con un patrón de reversión de velas.
- **Salida**: Stop loss y take profit calculados a partir del rango de la vela y multiplicadores de Fibonacci.
- **Largo/Corto**: Ambos.
- **Marco temporal**: Cualquiera.
- **Indicadores**: SMA.

Esta es una versión educativa simplificada. La lógica original de TradingView está reducida.
