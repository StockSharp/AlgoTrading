# Lacust Stop y BE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia demuestra la gestión básica de posiciones inspirada en el asesor experto MQL original **lacuststopandbe**.

Tras entrar en una posición en la dirección de la última vela completada, la estrategia aplica varias reglas de protección:

- El stop loss y el take profit iniciales se colocan a distancias de precio fijas.
- Cuando el beneficio alcanza `BreakevenGain`, el stop se mueve al precio de entrada más `Breakeven`.
- Después de que el beneficio supera `TrailingStart`, el stop sigue al precio a una distancia de `TrailingStop`.
- La posición se cierra cuando se toca el nivel de stop o el nivel de take profit.

Parámetros:

- `CandleType` – serie de velas usada para el procesamiento.
- `StopLoss` – distancia inicial del stop loss.
- `TakeProfit` – distancia inicial del take profit.
- `TrailingStart` – beneficio requerido para activar el trailing stop.
- `TrailingStop` – distancia del trailing stop desde el precio actual.
- `BreakevenGain` – beneficio requerido antes de mover el stop al break-even.
- `Breakeven` – beneficio asegurado después de mover el stop al break-even.

Este ejemplo utiliza la API de alto nivel de StockSharp y puede servir como plantilla para portar scripts simples de gestión de operaciones MQL.
