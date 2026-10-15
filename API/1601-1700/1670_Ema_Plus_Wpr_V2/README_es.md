# Estrategia EMA más WPR v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina el oscilador Williams %R con el filtro de tendencia EMA. Opera cuando WPR alcanza niveles extremos tras un retroceso. Incluye salidas opcionales basadas en WPR, trailing stops y salida basada en barras.

## Detalles

- **Largo**: WPR llega a -100 tras el retroceso y la tendencia EMA es alcista.
- **Corto**: WPR llega a 0 tras el retroceso y la tendencia EMA es bajista.
- **Indicadores**: Williams %R, EMA.
- **Stops**: stop-loss y take-profit fijos, trailing stop opcional.
