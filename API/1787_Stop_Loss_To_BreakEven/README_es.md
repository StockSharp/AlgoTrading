# Estrategia de Stop Loss al Punto de Equilibrio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia mueve el stop loss protector al precio de entrada una vez que la posición alcanza un beneficio especificado medido en pips. Es útil para asegurar ganancias sin ajustar manualmente las órdenes.

## Cómo funciona

- Monitorea el precio usando el tipo de vela seleccionado.
- Cuando el beneficio de la posición actual supera el número configurado de pips, se coloca una orden stop en el precio de entrada.
- Funciona tanto para posiciones largas como cortas y calcula automáticamente el tamaño del pip usando el paso de precio del instrumento.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| **BreakEvenPips** | Beneficio en pips requerido antes de mover el stop loss al precio de entrada. |
| **CandleType** | Tipo de velas utilizadas para monitorear los movimientos de precio. |

## Notas

La estrategia no genera señales de entrada. Las posiciones deben ser abiertas por otras estrategias o manualmente. Una vez cerrada la posición, el estado interno se reinicia para esperar la siguiente operación.
