# Estrategia de Perfil de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia simplificada de perfil de volumen rastrea el máximo, el mínimo de la sesión y el punto de control definido por el precio de la vela con mayor volumen. La estrategia compra cuando el precio está por encima del punto de control y vende cuando está por debajo. Las posiciones se cierran cuando el precio regresa al nivel medio de la sesión.

## Parámetros
- **Candle Type** – marco temporal de las velas de entrada.
