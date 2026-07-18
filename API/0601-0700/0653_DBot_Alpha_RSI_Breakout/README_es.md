# Estrategia Alpha RSI Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza SMA y RSI para capturar cruces del RSI por encima de un umbral cuando el precio está sobre la SMA. El trailing stop se activa después de que el RSI alcanza el nivel de take-profit. Sale por stop loss de RSI, al alcanzar el take-profit o por el trailing stop.

## Detalles

- **Datos**: velas de precio.
- **Entrada**: comprar cuando el RSI cruza por encima del nivel de entrada y el precio está por encima de la SMA.
- **Salida**: RSI por debajo del nivel de stop, RSI alcanza el take-profit, o el precio cae por debajo del trailing stop tras su activación.
- **Instrumentos**: cualquiera.
- **Riesgo**: stop loss basado en RSI y trailing stop tras obtener beneficios.
