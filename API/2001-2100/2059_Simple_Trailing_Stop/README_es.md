# Estrategia Simple de Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo demuestra cómo gestionar una posición abierta con un trailing stop usando la API de alto nivel de StockSharp.

## Descripción general
- Abre una única posición larga después de recibir la primera vela completada.
- Activa la protección de posición con un trailing stop.
- El precio del stop sigue al precio actual a una distancia fija.

## Parámetros
- `TrailPoints` – distancia en puntos de precio usada para seguir el stop.
- `CandleType` – tipo de velas procesadas por la estrategia.

## Lógica
1. Al iniciar, la estrategia se suscribe a velas y activa `StartProtection` con trailing.
2. Después de la primera vela completada, la estrategia compra a precio de mercado.
3. Cuando el precio se mueve a favor de la posición, el nivel del stop se mueve para mantener la distancia definida por `TrailPoints`.
4. Si el precio revierte y toca el trailing stop, la posición se cierra automáticamente.

La estrategia está simplificada y está pensada para mostrar el uso básico del trailing stop.
