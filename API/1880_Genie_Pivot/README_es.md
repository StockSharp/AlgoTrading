# Estrategia Genie Pivot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa la idea de scalping de reversión **Genie Pivot** originalmente escrita en MQL4. Espera un patrón de pivote formado por siete velas consecutivas y gestiona la posición abierta con un take profit fijo y un stop trailing.

## Lógica de la Estrategia

1. **Detección del patrón** – una señal larga aparece cuando los siete mínimos anteriores son estrictamente decrecientes y la última vela completada hace un mínimo más alto con un cierre por encima del máximo anterior. Una señal corta se genera por la condición espejo en los máximos.
2. **Ejecución de órdenes** – una vez confirmada la señal, la estrategia abre una orden de mercado con el volumen calculado a partir del patrimonio de la cuenta y los parámetros de riesgo configurados.
3. **Gestión de operaciones** – tras la entrada se establecen un take profit y un stop trailing. El stop trailing se activa solo una vez que el beneficio supera la distancia de trailing. Si el precio revierte en la siguiente vela (bajista para largo, alcista para corto), la posición se cierra inmediatamente.
4. **Reducción de volumen** – las operaciones perdedoras consecutivas reducen el volumen negociado según el parámetro `Decrease Factor`.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `TakeProfit` | Objetivo de ganancia en pasos de precio desde el precio de entrada. |
| `TrailingStop` | Distancia del stop trailing en pasos de precio. |
| `MaximumRisk` | Fracción del valor de la cuenta utilizada para dimensionar la posición. |
| `DecreaseFactor` | Reduce el volumen tras pérdidas consecutivas. |
| `BaseVolume` | Volumen de respaldo cuando el valor del portafolio es desconocido. |
| `CandleType` | Marco temporal de las velas a analizar. |

## Notas

La estrategia procesa únicamente velas completadas. No se ha proporcionado versión en Python todavía.
