# Estrategia de Rompimiento VWAP con ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra largo cuando el precio cruza por encima del VWAP y corto cuando cruza por debajo. El stop-loss y el take-profit se basan en múltiplos del ATR.

## Parámetros

- **AtrLength**: período del ATR.
- **StopAtrMultiplier**: multiplicador ATR para el stop-loss.
- **TakeAtrMultiplier**: multiplicador ATR para el take-profit.
- **CandleType**: tipo de velas.

