# Estrategia de Superposición de Código de Color
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera con cambios de color de velas usando un cálculo de código de color personalizado con stops fijos en pips.

## Lógica
- Construye velas de código de color personalizadas a partir de valores OHLC.
- Detecta cambios de color cuando el cuerpo supera el 1% del rango de la vela.
- Va largo en transición de rojo a verde, corto en transición de verde a rojo según el tipo de operación.
- Opera solo entre `StartTime` y `EndTime`.
- Aplica protecciones `StopLossPips` y `TakeProfitPips`.
