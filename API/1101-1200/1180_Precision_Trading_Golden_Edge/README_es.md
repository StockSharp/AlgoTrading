# Estrategia de Operación de Precisión: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de scalping para el Oro alinea el cruce de una EMA rápida y una EMA lenta con la dirección de una Hull Moving Average. Las operaciones ocurren solo cuando el RSI confirma el momentum y la volatilidad es adecuada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La EMA rápida cruza por encima de la EMA lenta, RSI > 55, HMA en alza, filtro de volatilidad pasa.
  - **Corto**: La EMA rápida cruza por debajo de la EMA lenta, RSI < 45, HMA en baja, filtro de volatilidad pasa.
- **Indicadores**: EMA, HMA, RSI, ATR, Highest/Lowest.
- **Tipo**: Seguimiento de tendencia.
- **Marco temporal**: Corto plazo.
