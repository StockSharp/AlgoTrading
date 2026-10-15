# Estrategia de Volumen por Punto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula el volumen por punto de precio para cada vela. Se abre una operación larga cuando el rango de la vela disminuye pero el volumen aumenta y el filtro RSI (si está activado) confirma la señal. Se abre una operación corta cuando el rango se expande mientras el volumen se contrae.

## Parámetros
- **RSI Length** – período para el cálculo del RSI.
- **RSI Above/Below** – umbrales para el filtro RSI opcional.
- **Use RSI Filter** – activar o desactivar el filtrado RSI.
- **Candle Type** – marco temporal de las velas de entrada.
