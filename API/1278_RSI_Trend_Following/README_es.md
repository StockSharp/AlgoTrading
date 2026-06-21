# Estrategia de Seguimiento de Tendencia RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Seguimiento de Tendencia RSI entra en largo cuando el momentum es confirmado por RSI, Estocástico, MACD y el precio permanece por encima de una EMA a largo plazo. Un stop de seguimiento se activa tras un movimiento favorable basado en ATR y sigue una EMA más corta.

Las posiciones se cierran cuando el precio cae por debajo de la EMA de seguimiento o alcanza el stop-loss basado en ATR.

## Detalles

- **Criterios de entrada**: `K < 80 && D < 80 && MACD > Signal && RSI > 50 && Low > EMA(200)`
- **Largo/Corto**: Solo largos
- **Criterios de salida**: Precio por debajo de la EMA de seguimiento o stop-loss
- **Stops**: Sí, basado en ATR
- **Valores predeterminados**:
  - `StopLossAtr` = 1.75
  - `TrailingActivationAtr` = 2.25
  - `RsiPeriod` = 14
  - `TrailingEmaLength` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
