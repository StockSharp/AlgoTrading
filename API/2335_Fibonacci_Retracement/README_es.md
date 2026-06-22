# Estrategia de Retroceso de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas de niveles de retroceso de Fibonacci derivados de pivotes ZigZag.

## Idea

1. Detectar máximos y mínimos de swing mediante un enfoque ZigZag.
2. Construir niveles de retroceso de Fibonacci (23.6%, 38.2%, 61.8%, 76.4%) entre los dos últimos pivotes.
3. En una tendencia alcista, la estrategia compra cuando el precio cierra por encima de cualquier nivel de Fibonacci.
4. En una tendencia bajista, la estrategia vende cuando el precio cierra por debajo de cualquier nivel de Fibonacci.
5. Cada orden está protegida con un stop-loss fijo y un take-profit basado en el rango del swing.
6. Tras el cierre de una posición, la estrategia espera un número de barras antes de volver a operar.

## Parámetros

- `ZigzagDepth` – profundidad utilizada para buscar nuevos pivotes.
- `SafetyBuffer` – distancia en puntos que el precio debe moverse más allá del nivel.
- `TrendPrecision` – diferencia mínima entre pivotes para detectar la dirección de la tendencia.
- `CloseBarPause` – número de barras a esperar tras cerrar una operación.
- `TakeProfitFactor` – fracción del rango del swing utilizada como extensión del take-profit.
- `StopLossPoints` – distancia del stop-loss desde el precio de entrada en puntos.
- `CandleType` – tipo de vela utilizado para los cálculos.

## Notas

Este archivo contiene solo la implementación en C#. Aún no se proporciona una versión en Python.
