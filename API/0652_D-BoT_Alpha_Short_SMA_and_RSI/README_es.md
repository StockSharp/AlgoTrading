# Estrategia D-BoT Alpha Short SMA y RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia corta que vende cuando el RSI cruza por encima de un umbral mientras el precio permanece por debajo de una media móvil simple. Un trailing stop sigue los nuevos mínimos y las posiciones se cierran si el RSI alcanza los niveles de stop o take-profit.

## Detalles

- **Criterios de entrada**: El RSI cruza por encima del nivel de entrada y el precio está por debajo de la SMA.
- **Criterios de salida**: El precio cruza por encima del trailing stop o el RSI alcanza los niveles de stop o take-profit.
