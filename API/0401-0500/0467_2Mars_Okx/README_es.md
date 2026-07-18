# Estrategia 2Mars OKX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un cruce de medias móviles con un filtro SuperTrend. Las Bollinger Bands proporcionan objetivos de beneficio mientras que un stop loss basado en ATR limita el riesgo.

## Reglas
- **Largo**: La EMA de señal cruza por encima de la EMA base y el precio está por encima del SuperTrend.
- **Corto**: La EMA de señal cruza por debajo de la EMA base y el precio está por debajo del SuperTrend.
- **Salida**: Toma de ganancias en la banda superior o inferior de Bollinger, o stop loss en ATR multiplicado por un factor.

## Indicadores
- EMA
- SuperTrend
- Bollinger Bands
- Average True Range
