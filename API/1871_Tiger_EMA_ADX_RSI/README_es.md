# Estrategia Tiger EMA ADX RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue la tendencia usando el cruce de dos medias móviles exponenciales (EMA) y filtra las operaciones con el Índice Direccional Promedio (ADX) y el Índice de Fuerza Relativa (RSI). La EMA rápida se compara con la EMA lenta para determinar la dirección de la tendencia. Las operaciones solo se permiten cuando el ADX supera un umbral configurable y el RSI se mantiene dentro de los límites superior e inferior.

Si no hay posición abierta y todas las condiciones se cumplen, la estrategia entra en la dirección de la tendencia. Cada entrada establece distancias fijas de take profit y stop loss desde el precio de entrada. La posición se cierra cuando se alcanza cualquier nivel. El volumen de la orden se define por la propiedad `Volume` de la estrategia.

## Parámetros

- **Fast EMA** – período de la media móvil exponencial rápida.
- **Slow EMA** – período de la media móvil exponencial lenta.
- **ADX Period** – período de cálculo del ADX.
- **ADX Threshold** – valor mínimo de ADX requerido para operar.
- **RSI Period** – período de cálculo del RSI.
- **RSI Upper** – valor máximo de RSI para entradas largas.
- **RSI Lower** – valor mínimo de RSI para entradas cortas.
- **Take Profit** – distancia desde el precio de entrada al take profit en puntos de precio.
- **Stop Loss** – distancia desde el precio de entrada al stop loss en puntos de precio.
- **Candle Type** – marco temporal u otro tipo de vela utilizado para los cálculos de indicadores.
