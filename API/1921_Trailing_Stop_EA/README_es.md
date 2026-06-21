# Estrategia de Trailing Stop EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia gestiona una posición existente aplicando un trailing stop. Escucha las operaciones tick a tick y desplaza el nivel de stop a medida que el precio se mueve en una dirección favorable. Cuando el mercado se revierte y alcanza el nivel del trailing stop, la estrategia cierra la posición.

## Detalles

- **Entrada**: La estrategia no abre posiciones; asume que ya existe una posición abierta.
- **Lógica largo**: Para posiciones largas, una vez que el precio sube la distancia del trailing, el stop sigue al precio hacia arriba.
- **Lógica corto**: Para posiciones cortas, el stop se desplaza hacia abajo a medida que el precio cae.
- **Salida**: La posición se cierra cuando el precio alcanza el trailing stop.
- **Indicadores**: Ninguno.
- **Marco temporal**: Basado en ticks, reacciona a cada operación.
- **Stops**: Solo trailing stop.

## Parámetros

- `TrailingPoints` — distancia del trailing stop en puntos (pasos de precio). Por defecto: 200.
