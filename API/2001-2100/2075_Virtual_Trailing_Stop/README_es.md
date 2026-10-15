# Stop Trailing Virtual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia emula un stop trailing virtual para posiciones largas y cortas. No genera señales de entrada; las órdenes deben abrirse externamente o manualmente. Una vez que existe una posición, la estrategia mantiene un stop trailing que sigue el precio a medida que se mueve en una dirección favorable. Si el precio alcanza el nivel del stop, la posición se cierra a mercado.

## Parámetros

- `StopLoss` – distancia fija del stop-loss en pasos de precio.
- `TakeProfit` – distancia fija del take-profit en pasos de precio.
- `TrailingStop` – distancia desde el precio actual hasta el stop trailing.
- `TrailingStart` – beneficio mínimo en pasos de precio antes de que comience el trailing.
- `TrailingStep` – beneficio adicional mínimo requerido para mover el nivel trailing.
- `CandleType` – serie de velas utilizada para procesar los datos de precio.

## Notas

La estrategia se suscribe a velas del tipo especificado y evalúa la lógica de trailing solo en velas cerradas.
