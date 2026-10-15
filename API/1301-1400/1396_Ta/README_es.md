# Estrategia Ta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce del MACD con pivotes de soporte y resistencia, confirmación de RSI y ADX. Se utilizan dos objetivos de beneficio con salida parcial.

## Detalles

- **Entrada**
  - **Largo**: MACD cruza por encima de la señal, precio por encima de la resistencia, RSI > 50, +DI > -DI, ADX > 20.
  - **Corto**: MACD cruza por debajo de la señal, precio por debajo del soporte, RSI < 50, -DI > +DI, ADX > 20.
- **Salida**: dos niveles de take-profit y un stop-loss.
