# Estrategia Sistema PChannel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El **Sistema PChannel** utiliza una ruptura de canal de precios con confirmación retardada. Rastrea el máximo más alto y el mínimo más bajo durante un período configurable. Cuando el precio rompe el canal y luego cierra de vuelta dentro, la estrategia entra en la dirección de la ruptura cerrando cualquier posición contraria. Los niveles opcionales de stop-loss y take-profit gestionan el riesgo.

## Parámetros
- `Period` – longitud de historial para el canal.
- `Shift` – número de barras para retrasar los valores del canal.
- `StopLoss` – distancia de precio absoluta para el stop de protección.
- `TakeProfit` – distancia de precio absoluta para el objetivo de beneficio.
- `CandleType` – serie de velas utilizada para los cálculos.

## Lógica de Trading
1. Calcular los límites del canal a partir de las últimas `Period` velas con un `Shift` opcional.
2. Si la vela anterior cerró fuera del canal y la vela actual regresa dentro, abrir una posición en la dirección del rompimiento.
3. Cerrar la posición opuesta, si la hay, antes de abrir una nueva.
4. Monitorear las operaciones activas y salir cuando se alcanza `StopLoss` o `TakeProfit`.

Esta estrategia aún no tiene implementación en Python.
