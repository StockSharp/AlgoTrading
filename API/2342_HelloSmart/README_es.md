# Estrategia HelloSmart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un enfoque de trading en cuadrícula simple que abre posiciones en una sola dirección. Se coloca una nueva orden cada vez que el mercado se mueve un número configurado de ticks en contra de la última entrada. Cuando el volumen acumulado de la posición alcanza un umbral, el tamaño de la siguiente orden se multiplica. Todas las posiciones se cierran cuando la ganancia o pérdida total alcanza los límites predefinidos.

## Parámetros
- **Trade Direction** – elegir 1 para abrir solo posiciones largas o 2 para abrir solo posiciones cortas.
- **Step** – número de ticks de precio que el mercado debe moverse antes de añadir otra posición.
- **Initial Lot** – volumen base para la primera orden.
- **Threshold Volume** – tamaño de posición acumulada que activa la multiplicación del lote.
- **Maximum Lot** – límite superior para el volumen de cualquier orden individual.
- **Profit Target** – monto de ganancia en divisa tras el cual se cierran todas las posiciones.
- **Loss Limit** – monto de pérdida en divisa tras el cual se cierran todas las posiciones.
- **Lot Multiplier** – factor aplicado a la siguiente orden cuando se supera el volumen umbral.
- **Candle Type** – serie de velas usada para medir el movimiento de precios.
