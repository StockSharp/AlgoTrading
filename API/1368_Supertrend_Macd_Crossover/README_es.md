# Estrategia de Cruce MACD con Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el indicador Supertrend con un cruce de MACD para identificar entradas alcistas.
Se abre una posición larga cuando el precio está por encima de la línea Supertrend y la línea MACD cruza por encima de su línea de señal.
La posición se cierra cuando el precio cae por debajo de la línea Supertrend y la línea MACD cruza por debajo de su señal.

## Detalles

- **Indicadores**: Supertrend (ATR 10, factor 3), MACD (12, 26, 9)
- **Entrada**: Precio por encima de Supertrend y cruce alcista de MACD
- **Salida**: Precio por debajo de Supertrend y cruce bajista de MACD
- **Dirección**: Solo largos
- **Marco temporal**: Cualquiera
