# Estrategia Simple de Pull Back TJlv26
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra cuando el precio está por encima de la SMA larga, por debajo de la SMA corta y el RSI(3) está por debajo de 30 dentro de un rango de fechas especificado. Sale con stop-loss y take-profit basados en porcentajes o cuando el precio está por encima de la SMA corta pero por debajo del mínimo de la vela anterior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre > SMA larga, Cierre < SMA corta, RSI(3) < 30, tiempo entre StartDate y EndDate.
- **Criterios de salida**:
  - Stop loss: precio ≤ precio de entrada × (1 − StopLossPercent/100).
  - Take profit: precio ≥ precio de entrada × (1 + TakeProfitPercent/100).
  - Cerrar si precio > SMA corta y precio < mínimo de la vela anterior.
- **Indicadores**: SMA, RSI.
- **Stops**: Sí.
- **Dirección**: Solo largos.
