# Estrategia de Activación de Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Activación de Trailing Stop** gestiona los niveles de stop de protección para las posiciones existentes. No genera señales de entrada; en cambio, ajusta los stops después de que se abre una posición para asegurar las ganancias.

## Parámetros

- `TrailingStop` – distancia en unidades de precio que el mercado debe moverse a favor de la posición antes de que se active el trailing stop.
- `StopLoss` – distancia inicial de stop-loss en unidades de precio (opcional). Establezca `0` para desactivar.
- `CandleType` – tipo de velas usadas para el seguimiento del precio.

## Reglas de operación

1. Cuando se abre una posición, se coloca un stop-loss inicial si `StopLoss` es mayor que cero.
2. Una vez que el beneficio supera `TrailingStop`, el nivel de stop sigue al precio manteniendo la distancia especificada.
3. La posición se cierra cuando el precio toca el nivel de trailing stop.
4. La estrategia funciona tanto para posiciones largas como cortas.

## Notas

Esta estrategia está diseñada para usarse junto a otra estrategia que proporcione señales de entrada. Se centra únicamente en la gestión de salidas mediante trailing stops.
