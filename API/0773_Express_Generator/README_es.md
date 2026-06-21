# Estrategia Express Generator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en cruces de medias móviles confirmados por señales de RSI y MACD. El tamaño de posición utiliza un factor de volatilidad basado en ATR y un porcentaje de riesgo fijo. Un stop trailing en pips gestiona las salidas.

## Detalles

- **Entrada Largo**: La SMA rápida cruza por encima de la SMA lenta, RSI por debajo de sobrecompra, la línea MACD cruza por encima de la señal.
- **Entrada Corto**: La SMA rápida cruza por debajo de la SMA lenta, RSI por encima de sobreventa, la línea MACD cruza por debajo de la señal.
- **Salida**: Stop trailing en pips.
- **Tamaño de posición**: % de riesgo sobre el capital dividido por la distancia del stop ajustada por ATR.
- **Indicadores**: SMA, RSI, MACD, ATR.
- **Dirección**: Ambas direcciones.
